using BloodDonationManagement.Data;
using BloodDonationManagement.Hubs;
using BloodDonationManagement.Models;
using BloodDonationManagement.Services;
using BloodDonationManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers
{
    public class BloodRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly DonorMatchingService _matching;
        private readonly EligibilityService _eligibility;
        private readonly NotificationService _notifications;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _audit;

        public BloodRequestController(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hub,
            DonorMatchingService matching,
            EligibilityService eligibility,
            NotificationService notifications,
            UserManager<IdentityUser> userManager,
            AuditService audit)
        {
            _audit = audit;
            _context = context;
            _hub = hub;
            _matching = matching;
            _eligibility = eligibility;
            _notifications = notifications;
            _userManager = userManager;
        }

        private string? CurrentUserId => _userManager.GetUserId(User);
        private bool IsAdmin => User.IsInRole("Admin");

        private bool CanManage(BloodRequest r) =>
            IsAdmin || (!string.IsNullOrEmpty(r.RequesterUserId) && r.RequesterUserId == CurrentUserId);

        /// <summary>
        /// Privacy rule: patient name and requester phone are visible only to the requester, admins,
        /// and donors who have accepted that request.
        /// </summary>
        private async Task<HashSet<int>> ContactVisibleRequestIdsAsync(List<int> requestIds)
        {
            if (CurrentUserId == null || requestIds.Count == 0) return new HashSet<int>();
            if (IsAdmin) return requestIds.ToHashSet();

            var own = await _context.BloodRequests
                .Where(r => requestIds.Contains(r.BloodRequestId) && r.RequesterUserId == CurrentUserId)
                .Select(r => r.BloodRequestId).ToListAsync();
            var accepted = await _context.DonorResponses
                .Where(x => requestIds.Contains(x.BloodRequestId) && x.Donor!.UserId == CurrentUserId
                            && (x.Status == ResponseStatus.Accepted || x.Status == ResponseStatus.Confirmed))
                .Select(x => x.BloodRequestId).ToListAsync();
            return own.Concat(accepted).ToHashSet();
        }

        // ------------------------------------------------------------------ List

        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            const int pageSize = 20;
            await ExpireOverdueRequestsAsync();

            var query = _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .Include(r => r.Responses)
                .AsQueryable();

            query = status switch
            {
                "open" => query.Where(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.DonorMatched),
                "fulfilled" => query.Where(r => r.Status == RequestStatus.Fulfilled),
                "closed" => query.Where(r => r.Status == RequestStatus.Cancelled || r.Status == RequestStatus.Expired),
                "mine" when CurrentUserId != null => query.Where(r => r.RequesterUserId == CurrentUserId),
                _ => query
            };

            var ordered = query
                .OrderBy(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.DonorMatched ? 0 : 1)
                .ThenByDescending(r => r.Urgency)
                .ThenByDescending(r => r.CreatedDate);

            var totalCount = await ordered.CountAsync();
            var requests = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.VisibleIds = await ContactVisibleRequestIdsAsync(requests.Select(r => r.BloodRequestId).ToList());
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(requests);
        }

        // ------------------------------------------------------------------ Details + Smart Matching

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            await ExpireOverdueRequestsAsync();

            var request = await _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .Include(r => r.Responses).ThenInclude(x => x.Donor)
                .FirstOrDefaultAsync(m => m.BloodRequestId == id);
            if (request == null) return NotFound();

            var vm = new RequestDetailsViewModel
            {
                Request = request,
                CanManage = CanManage(request),
                IsAdmin = IsAdmin,
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                Responses = request.Responses.OrderByDescending(r => r.Status == ResponseStatus.Confirmed)
                                             .ThenByDescending(r => r.Status == ResponseStatus.Accepted)
                                             .ThenByDescending(r => r.MatchScore).ToList(),
                ConfirmedUnits = request.Responses.Count(r => r.Status == ResponseStatus.Confirmed)
            };

            if (vm.CanManage && request.IsOpen)
                vm.Match = await _matching.FindMatchesAsync(request);

            if (CurrentUserId != null)
            {
                vm.MyDonor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == CurrentUserId);
                if (vm.MyDonor != null)
                {
                    vm.MyResponse = request.Responses.FirstOrDefault(r => r.DonorId == vm.MyDonor.DonorId);
                    vm.MyEligibility = _eligibility.Evaluate(vm.MyDonor);
                    vm.MyGroupCompatible = BloodCompatibilityService.CanDonateToRecipient(vm.MyDonor.BloodGroup, request.BloodGroup);
                }
            }

            vm.CanSeeContact = vm.CanManage
                || vm.MyResponse?.Status is ResponseStatus.Accepted or ResponseStatus.Confirmed;
            vm.Timeline = BuildTimeline(request);
            vm.IsOwner = !string.IsNullOrEmpty(request.RequesterUserId) && request.RequesterUserId == CurrentUserId;
            if (CurrentUserId != null)
                vm.AlreadyReported = await _context.RequestReports.AnyAsync(r => r.BloodRequestId == request.BloodRequestId && r.ReporterUserId == CurrentUserId);
            if (vm.IsAdmin)
                vm.OpenReports = await _context.RequestReports.CountAsync(r => r.BloodRequestId == request.BloodRequestId && r.Status == ReportStatus.Open);
            return View(vm);
        }

        private static List<TimelineItem> BuildTimeline(BloodRequest r)
        {
            var items = new List<TimelineItem>
            {
                new(r.CreatedDate, "Request created", $"{r.BloodGroup} · {r.UnitsNeeded} unit(s) · {r.Urgency}", "fa-plus", "tl-primary")
            };

            var notified = r.Responses.Where(x => x.NotifiedAt != default).ToList();
            if (notified.Count > 0)
                items.Add(new(notified.Min(x => x.NotifiedAt), "Donors notified",
                    $"{notified.Count} matched donor(s) alerted", "fa-bell", "tl-info"));

            foreach (var a in r.Responses.Where(x => x.RespondedAt.HasValue && x.Status != ResponseStatus.Declined))
                items.Add(new(a.RespondedAt!.Value, "Donor accepted", $"{a.Donor?.FullName} can donate", "fa-hand-holding-heart", "tl-success"));

            foreach (var c in r.Responses.Where(x => x.ConfirmedAt.HasValue))
                items.Add(new(c.ConfirmedAt!.Value, "Donation confirmed", $"{c.Donor?.FullName} donated", "fa-check-circle", "tl-success"));

            if (r.Status == RequestStatus.Fulfilled && r.FulfilledAt.HasValue)
                items.Add(new(r.FulfilledAt.Value, "Request fulfilled", "All units received", "fa-flag-checkered", "tl-success"));
            if (r.Status == RequestStatus.Cancelled)
                items.Add(new(r.FulfilledAt ?? DateTime.Now, "Request cancelled", "", "fa-ban", "tl-muted"));
            if (r.Status == RequestStatus.Expired)
                items.Add(new(r.NeededBy ?? DateTime.Now, "Request expired", "Needed-by time passed", "fa-hourglass-end", "tl-muted"));

            return items.OrderBy(i => i.At).ToList();
        }

        // ------------------------------------------------------------------ Create

        [Authorize]
        public IActionResult Create()
        {
            LoadLocationLists(null, null);
            return View(new BloodRequest { NeededBy = DateTime.Now.AddHours(24) });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BloodRequest request)
        {
            // Basic anti-spam: limit open requests per user
            var openCount = await _context.BloodRequests.CountAsync(r => r.RequesterUserId == CurrentUserId
                && (r.Status == RequestStatus.Pending || r.Status == RequestStatus.DonorMatched));
            const int maxOpenRequests = 5;
            if (!IsAdmin && openCount >= maxOpenRequests)
                ModelState.AddModelError(string.Empty, $"You already have {maxOpenRequests} open requests. Please close or fulfil one before creating another.");

            if (!ModelState.IsValid)
            {
                LoadLocationLists(request.DistrictId, request.ThanaId);
                return View(request);
            }

            request.BloodGroup = request.BloodGroup.Trim().ToUpperInvariant();
            request.RequesterUserId = CurrentUserId;
            request.CreatedDate = DateTime.Now;
            request.Status = RequestStatus.Pending;
            _context.Add(request);
            await _context.SaveChangesAsync();

            // Smart Matching: automatically notify the top-ranked donors
            var match = await _matching.FindMatchesAsync(request);
            var notifiedCount = await NotifyDonorsAsync(request, match.Matches.Take(_matching.Options.NotifyTopN));
            await _audit.LogAsync(AuditActions.RequestCreated, "Request", request.BloodRequestId,
                $"Posted {request.Urgency} {request.BloodGroup} request ({request.UnitsNeeded} unit/s); {match.Matches.Count} matched, {notifiedCount} alerted");

            // Live broadcast to everyone watching this district (public feed)
            var district = request.DistrictId.HasValue ? await _context.Districts.FindAsync(request.DistrictId.Value) : null;
            var payload = new
            {
                request.BloodRequestId,
                request.BloodGroup,
                District = district?.Name ?? "Unknown",
                Urgency = request.Urgency.ToString(),
                Hospital = request.HospitalName ?? ""
            };
            if (request.DistrictId.HasValue)
                await _hub.Clients.Group($"district_{request.DistrictId.Value}").SendAsync("NewBloodRequest", payload);
            else
                await _hub.Clients.All.SendAsync("NewBloodRequest", payload);

            TempData["SuccessMessage"] = notifiedCount > 0
                ? $"Request created. Smart Matching found {match.Matches.Count} donor(s) and alerted the top {notifiedCount}."
                : $"Request created. Smart Matching found {match.Matches.Count} eligible donor(s) nearby.";
            return RedirectToAction(nameof(Details), new { id = request.BloodRequestId });
        }

        // ------------------------------------------------------------------ Notify donors (requester / admin)

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Notify(int id, int donorId)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!CanManage(request)) return Forbid();
            if (!request.IsOpen) return RedirectToAction(nameof(Details), new { id });

            var match = await _matching.FindMatchesAsync(request);
            var target = match.Matches.FirstOrDefault(m => m.Donor.DonorId == donorId);
            if (target == null)
            {
                TempData["ErrorMessage"] = "This donor is no longer eligible for this request.";
            }
            else
            {
                var n = await NotifyDonorsAsync(request, new[] { target });
                TempData[n > 0 ? "SuccessMessage" : "InfoMessage"] =
                    n > 0 ? $"{target.Donor.FullName} has been notified." : "This donor was already notified.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Escalation wave: alert the next group of ranked donors who were not yet notified.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyNext(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!CanManage(request)) return Forbid();

            var match = await _matching.FindMatchesAsync(request);
            var next = match.Matches.Where(m => m.CurrentResponse == null).Take(_matching.Options.NotifyTopN * 2);
            var n = await NotifyDonorsAsync(request, next);
            TempData[n > 0 ? "SuccessMessage" : "InfoMessage"] =
                n > 0 ? $"Escalation: {n} more donor(s) notified." : "All matched donors have already been notified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<int> NotifyDonorsAsync(BloodRequest request, IEnumerable<DonorMatch> matches)
        {
            var already = await _context.DonorResponses
                .Where(r => r.BloodRequestId == request.BloodRequestId)
                .Select(r => r.DonorId)
                .ToListAsync();

            var toNotify = matches.Where(m => !already.Contains(m.Donor.DonorId)).ToList();
            foreach (var m in toNotify)
            {
                _context.DonorResponses.Add(new DonorResponse
                {
                    BloodRequestId = request.BloodRequestId,
                    DonorId = m.Donor.DonorId,
                    Status = ResponseStatus.Notified,
                    MatchScore = m.Score,
                    DistanceKm = m.DistanceKm.HasValue ? Math.Round(m.DistanceKm.Value, 2) : null,
                    NotifiedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();

            var link = Url.Action(nameof(Details), "BloodRequest", new { id = request.BloodRequestId });
            var absoluteLink = Url.Action(nameof(Details), "BloodRequest", new { id = request.BloodRequestId }, Request.Scheme);
            foreach (var m in toNotify.Where(m => !string.IsNullOrEmpty(m.Donor.UserId)))
            {
                await _notifications.NotifyAsync(
                    m.Donor.UserId,
                    $"{request.Urgency} request: {request.BloodGroup} needed",
                    $"{request.HospitalName ?? "A patient"} needs {request.UnitsNeeded} unit(s). You are a {m.Score:0}% match.",
                    link,
                    request.Urgency == UrgencyLevel.Critical ? "danger" : request.Urgency == UrgencyLevel.Urgent ? "warning" : "info",
                    $"[{request.Urgency}] {request.BloodGroup} blood needed — you are a {m.Score:0}% match",
                    EmailTemplates.RequestAlert(request, m.Score, absoluteLink));
            }
            return toNotify.Count;
        }

        // ------------------------------------------------------------------ Donor answers (Accept / Decline)

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, bool accept)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!request.IsOpen)
            {
                TempData["ErrorMessage"] = "This request is already closed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == CurrentUserId);
            if (donor == null)
            {
                TempData["InfoMessage"] = "Please create your donor profile first.";
                return RedirectToAction("Create", "Donor");
            }
            if (!BloodCompatibilityService.CanDonateToRecipient(donor.BloodGroup, request.BloodGroup))
            {
                TempData["ErrorMessage"] = $"Your blood group ({donor.BloodGroup}) is not compatible with {request.BloodGroup}.";
                return RedirectToAction(nameof(Details), new { id });
            }
            var eligibility = _eligibility.Evaluate(donor);
            if (accept && !eligibility.IsEligible)
            {
                TempData["ErrorMessage"] = $"You can donate again after {eligibility.NextEligibleDate:dd MMM yyyy}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var response = await _context.DonorResponses
                .FirstOrDefaultAsync(r => r.BloodRequestId == id && r.DonorId == donor.DonorId);
            if (response == null)
            {
                // Donor found the request themself (not notified): record score for transparency
                var match = await _matching.FindMatchesAsync(request);
                var m = match.Matches.FirstOrDefault(x => x.Donor.DonorId == donor.DonorId);
                response = new DonorResponse
                {
                    BloodRequestId = id,
                    DonorId = donor.DonorId,
                    MatchScore = m?.Score ?? 0,
                    DistanceKm = m?.DistanceKm,
                    NotifiedAt = DateTime.Now
                };
                _context.DonorResponses.Add(response);
            }
            if (response.Status == ResponseStatus.Confirmed)
                return RedirectToAction(nameof(Details), new { id });

            response.Status = accept ? ResponseStatus.Accepted : ResponseStatus.Declined;
            response.RespondedAt = DateTime.Now;
            if (accept && request.Status == RequestStatus.Pending)
                request.Status = RequestStatus.DonorMatched;
            await _context.SaveChangesAsync();

            if (accept)
            {
                await _audit.LogAsync(AuditActions.ContactShared, "Request", id,
                    $"Donor {donor.FullName} ({donor.BloodGroup}) accepted request #{id}; contact details shared between donor and requester");
                await _notifications.NotifyAsync(
                    request.RequesterUserId,
                    "A donor accepted your request",
                    $"{donor.FullName} ({donor.BloodGroup}) can donate for {request.PatientName}. Contact details are now visible.",
                    Url.Action(nameof(Details), "BloodRequest", new { id }),
                    "success");
                TempData["SuccessMessage"] = "Thank you! The requester can now see your contact number and will call you.";
            }
            else
            {
                TempData["InfoMessage"] = "No problem — we will not contact you for this request.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------ Confirm donation (requester / admin)

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDonation(int id, int donorId)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!CanManage(request)) return Forbid();

            var response = await _context.DonorResponses
                .Include(r => r.Donor)
                .FirstOrDefaultAsync(r => r.BloodRequestId == id && r.DonorId == donorId);
            if (response?.Donor == null || response.Status != ResponseStatus.Accepted)
            {
                TempData["ErrorMessage"] = "Only donors who accepted can be confirmed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            response.Status = ResponseStatus.Confirmed;
            response.ConfirmedAt = DateTime.Now;

            // Auto-update the donor's history (FR-3.4)
            response.Donor.LastDonationDate = DateTime.Today;
            _context.Donations.Add(new Donation
            {
                DonorId = donorId,
                BloodRequestId = id,
                DonationDate = DateTime.Today,
                Units = 1,
                HospitalName = request.HospitalName
            });

            var confirmed = await _context.DonorResponses.CountAsync(r => r.BloodRequestId == id && r.Status == ResponseStatus.Confirmed) + 1;
            if (confirmed >= request.UnitsNeeded)
            {
                request.Status = RequestStatus.Fulfilled;
                request.FulfilledAt = DateTime.Now;
            }
            else
            {
                request.Status = RequestStatus.DonorMatched;
            }
            await _context.SaveChangesAsync();

            await _audit.LogAsync(AuditActions.DonationConfirmed, "Request", id,
                $"Confirmed donation by {response.Donor.FullName} for request #{id} ({confirmed}/{request.UnitsNeeded} units)");

            await _notifications.NotifyAsync(
                response.Donor.UserId,
                "Thank you, lifesaver! ❤️",
                $"Your donation for {request.PatientName} has been confirmed. You can donate again after {_eligibility.Evaluate(response.Donor).NextEligibleDate:dd MMM yyyy}.",
                Url.Action("MyProfile", "Donor"),
                "success");

            TempData["SuccessMessage"] = request.Status == RequestStatus.Fulfilled
                ? "Donation confirmed — request fulfilled!"
                : $"Donation confirmed ({confirmed}/{request.UnitsNeeded} units).";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!CanManage(request)) return Forbid();
            if (request.IsOpen)
            {
                request.Status = RequestStatus.Cancelled;
                request.FulfilledAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await _audit.LogAsync(AuditActions.RequestCancelled, "Request", id,
                    IsAdmin && request.RequesterUserId != CurrentUserId ? $"Admin cancelled request #{id}" : $"Requester cancelled request #{id}");
                TempData["InfoMessage"] = "Request cancelled.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------ Report (trust & safety)

        public static readonly string[] ReportReasons =
        {
            "Asking for money", "Fake or suspicious request", "Wrong or misleading information",
            "Already fulfilled", "Abusive content", "Other"
        };

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(int id, string reason, string? details)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (!ReportReasons.Contains(reason))
            {
                TempData["ErrorMessage"] = "Please choose a reason.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (request.RequesterUserId == CurrentUserId)
                return RedirectToAction(nameof(Details), new { id });

            var already = await _context.RequestReports.AnyAsync(r => r.BloodRequestId == id && r.ReporterUserId == CurrentUserId);
            if (already)
            {
                TempData["InfoMessage"] = "You have already reported this request. Our admins are reviewing it.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.RequestReports.Add(new RequestReport
            {
                BloodRequestId = id,
                ReporterUserId = CurrentUserId!,
                Reason = reason,
                Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim()[..Math.Min(details.Trim().Length, 500)]
            });
            await _context.SaveChangesAsync();

            await _audit.LogAsync(AuditActions.RequestReported, "Request", id, $"Reported request #{id}: {reason}");

            // Alert all admins
            var link = Url.Action("Reports", "Admin");
            foreach (var admin in await _userManager.GetUsersInRoleAsync("Admin"))
                await _notifications.NotifyAsync(admin.Id, "Request reported", $"{request.BloodGroup} request #{id}: {reason}", link, "warning");

            TempData["SuccessMessage"] = "Thank you. The request has been reported and an admin will review it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ------------------------------------------------------------------ Admin

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            LoadLocationLists(request.DistrictId, request.ThanaId);
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BloodRequest input)
        {
            if (id != input.BloodRequestId) return NotFound();
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (!ModelState.IsValid)
            {
                LoadLocationLists(input.DistrictId, input.ThanaId);
                return View(input);
            }

            // Copy only editable fields (keeps RequesterUserId, CreatedDate, etc.)
            request.PatientName = input.PatientName;
            request.BloodGroup = input.BloodGroup.Trim().ToUpperInvariant();
            request.ContactNo = input.ContactNo;
            request.HospitalName = input.HospitalName;
            request.DistrictId = input.DistrictId;
            request.ThanaId = input.ThanaId;
            request.Latitude = input.Latitude;
            request.Longitude = input.Longitude;
            request.Message = input.Message ?? string.Empty;
            request.Urgency = input.Urgency;
            request.UnitsNeeded = input.UnitsNeeded;
            request.NeededBy = input.NeededBy;
            request.Status = input.Status;
            request.IsVerified = input.IsVerified;

            await _context.SaveChangesAsync();
            await _audit.LogAsync(AuditActions.RequestEdited, "Request", id,
                $"Edited request #{id} (status {request.Status}, urgency {request.Urgency}, verified {request.IsVerified})");
            TempData["SuccessMessage"] = "Blood request updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVerified(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request == null) return NotFound();
            request.IsVerified = !request.IsVerified;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(request.IsVerified ? AuditActions.RequestVerified : AuditActions.RequestUnverified, "Request", id,
                request.IsVerified ? $"Marked request #{id} as verified" : $"Removed verification from request #{id}");
            TempData["SuccessMessage"] = request.IsVerified ? "Request marked as verified." : "Verification removed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var request = await _context.BloodRequests
                .Include(r => r.District)
                .Include(r => r.Thana)
                .FirstOrDefaultAsync(m => m.BloodRequestId == id);
            if (request == null) return NotFound();
            return View(request);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request != null)
            {
                var summary = $"Deleted request #{id} ({request.BloodGroup}, {request.HospitalName ?? "no hospital"}, status {request.Status})";
                _context.BloodRequests.Remove(request);
                await _context.SaveChangesAsync();
                await _audit.LogAsync(AuditActions.RequestDeleted, "Request", id, summary);
                TempData["SuccessMessage"] = "Blood request deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var request = await _context.BloodRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = RequestStatus.Fulfilled;
                request.FulfilledAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await _audit.LogAsync(AuditActions.RequestFulfilledByAdmin, "Request", id, $"Admin marked request #{id} as fulfilled");
                TempData["SuccessMessage"] = "Blood request marked as fulfilled!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------ Helpers

        private async Task ExpireOverdueRequestsAsync()
        {
            var now = DateTime.Now;
            var overdue = await _context.BloodRequests
                .Where(r => (r.Status == RequestStatus.Pending || r.Status == RequestStatus.DonorMatched)
                            && r.NeededBy != null && r.NeededBy < now)
                .ToListAsync();
            if (overdue.Count == 0) return;
            foreach (var r in overdue) r.Status = RequestStatus.Expired;
            await _context.SaveChangesAsync();
        }

        private void LoadLocationLists(int? districtId, int? thanaId)
        {
            ViewData["DistrictId"] = new SelectList(_context.Districts.OrderBy(d => d.Name), "DistrictId", "Name", districtId);
            ViewData["ThanaId"] = new SelectList(
                districtId.HasValue ? _context.Thanas.Where(t => t.DistrictId == districtId.Value).OrderBy(t => t.Name) : Enumerable.Empty<Thana>(),
                "ThanaId", "Name", thanaId);
        }
    }
}
