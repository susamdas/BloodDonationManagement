# Business Requirements Document (BRD)
## BloodConnect BD — Smart Blood Donation Management System

| Item | Detail |
|---|---|
| Project | BloodConnect BD (ASP.NET Core MVC, .NET 8, SQL Server) |
| Purpose | Masters (IT) project — application build phase |
| Version | 1.0 (draft) |
| Date | 18 November 2026 |
| Build window | 2 days development + 1 day buffer |
| Status | Awaiting confirmation before coding |

---

## 1. Background and Objective

In Bangladesh, people who need blood mostly rely on Facebook groups, personal networks and phone calls. This is slow, unverified and exposes donors' personal data. Existing apps mostly provide a **searchable donor list**. They do not decide *who is the best donor to contact first*, they do not check whether the donor is *eligible today*, and they do not follow up when nobody responds.

**Objective:** Build a modern web platform that **finds, ranks and notifies the right donors automatically**, protects donor privacy, and tracks every request from creation to fulfilment.

---

## 2. Market Research — Problems Found in Existing Solutions

| ID | Problem observed | Evidence (see §12 Sources) | Our solution |
|---|---|---|---|
| **P1** | **No verified platform; fake requests and money scams.** Syndicates in Facebook groups demand money (e.g. via bKash). Scammers exploit urgent appeals for rare negative groups | Dhaka Tribune; TBS News | Request verification, "Never pay for blood" safeguard, report/flag requests, admin moderation (FR-6) |
| **P2** | **Donor privacy is exposed.** Some apps display donors' name, phone number and address to anyone searching | Dhaka Tribune (BloodHouse); Facebook's own feature hides donor data by design | Contact details masked until the donor accepts (FR-5) |
| **P3** | **Stale data / ineligible donors get contacted.** Lists do not know who donated recently or who is unavailable | UTHM 2025 (apps "lack updated information") | Automatic eligibility cooldown, "Available now" toggle, freshness check (FR-2) |
| **P4** | **Weak notifications and communication gaps** | UTHM 2025 (gaps in push notifications and donor engagement) | Real-time alerts + email, with **escalation waves** when no one responds (FR-4) |
| **P5** | **Plain list search, no prioritisation.** Requester must call donors one by one | Common pattern across reviewed apps | **Smart Donor Matching & Ranking** with explainable score (FR-1) |
| **P6** | **Recurring patients (e.g. thalassemia) lack donors on the scheduled date.** Patients often need transfusions every 2–5 weeks | My Hema Circle; Acibadem | Recurring requests and a "Donor Circle" of pledged donors (FR-8) |
| **P7** | **No feedback loop.** Requester cannot track progress; donation history is never updated | General app reviews; UTHM 2025 (tracking limitations) | Request timeline + two-sided confirmation that updates donor history automatically (FR-3) |
| **P8** | **Low donor engagement and retention** | Kennesaw systematic review; UTHM 2025 | Reliability score, badges, certificate, "You can donate again" reminders (FR-7) |

---

## 3. Stakeholders and Roles

| Role | Description | Main goals |
|---|---|---|
| **Donor** | Registered user who can give blood | Get relevant requests only; keep privacy; track own donations |
| **Requester** | Patient, relative or hospital staff (logged-in user) | Find a suitable donor quickly; track request |
| **Admin** | System operator | Moderate requests and users; view analytics; manage master data |
| **Guest** | Not logged in | View public information and awareness content |

> A single user account can be both a Donor and a Requester.

---

## 4. Current State (Already Built)

| Area | Status |
|---|---|
| Registration / login (ASP.NET Identity), roles Admin/User | ✅ Built |
| Donor CRUD with blood group, district/thana, lat/long | ✅ Built |
| Blood requests with urgency (Normal/Urgent/Critical), hospital, status (Pending/Fulfilled) | ✅ Built |
| Search by blood group and location | ✅ Built |
| Blood compatibility table, Haversine distance | ✅ Built (basic) |
| Map page, email service, SignalR hub | ✅ Built (basic) |
| District/Thana master data (64 / 500) | ✅ Built |

### Known gaps / defects to fix

| ID | Issue |
|---|---|
| D1 | Fresh database crashes on startup — `DataSeeder` inserts explicit IDs into an identity column |
| D2 | Connection string is machine-specific; app fails on other PCs |
| D3 | `Donor` is not linked to the logged-in user account (needed for "I can donate" flow) |
| D4 | `Donor.Validate` rejects a *recent* last-donation date — donors cannot record their true date. A recent date should be **allowed** and simply make the donor ineligible |
| D5 | Admin user management (activate/deactivate) promised in README but missing |
| D6 | Google Maps API key must not be stored in tracked files (repo is on GitHub) |

---

## 5. Functional Requirements

Priority: **M** = Must (2-day scope) · **S** = Should (if time) · **C** = Could (future)

### FR-1 Smart Donor Matching & Ranking ⭐ (core contribution)

| ID | Requirement | Priority |
|---|---|---|
| FR-1.1 | When a request is created, the system automatically finds candidate donors | M |
| FR-1.2 | Hard filters remove incompatible, ineligible, unavailable and deactivated donors | M |
| FR-1.3 | Remaining donors receive a **Match Score (0–100%)** using the weighted model in §6 | M |
| FR-1.4 | Each result shows **explanation chips**, e.g. `Exact match · 2.4 km · Eligible (140 days) · 85% response` | M |
| FR-1.5 | Search radius expands automatically by urgency (Critical searches wider) | M |
| FR-1.6 | Rare negative groups (O−, A−, B−, AB−) are flagged and prioritised | S |
| FR-1.7 | Weights are configurable in settings (no code change needed) | S |

### FR-2 Donor Eligibility and Availability

| ID | Requirement | Priority |
|---|---|---|
| FR-2.1 | Add **Gender** to donor profile | M |
| FR-2.2 | Automatic cooldown: **90 days (male)**, **120 days (female)** since last donation (configurable) | M |
| FR-2.3 | Donor sees eligibility status and "Next eligible date" on their profile | M |
| FR-2.4 | **"Available now" toggle** — donor can pause alerts (travel, illness) | M |
| FR-2.5 | Basic self-check before accepting (age 18–60, weight ≥ 50 kg, feeling well) | S |
| FR-2.6 | Freshness check: if a profile is not updated for 6 months, ask "Are you still available?" | C |

### FR-3 Request Lifecycle and Tracking

| ID | Requirement | Priority |
|---|---|---|
| FR-3.1 | Status flow: **Pending → Donor Matched → Fulfilled**, plus **Cancelled / Expired** | M |
| FR-3.2 | Donor can **Accept ("I can donate")** or **Decline** a request | M |
| FR-3.3 | Requester sees the list of donors who accepted and picks/confirms one | M |
| FR-3.4 | On fulfilment, the donor's **LastDonationDate updates automatically** and a donation history record is created | M |
| FR-3.5 | Request **timeline** (created → notified → accepted → fulfilled) with timestamps | S |
| FR-3.6 | Requests auto-expire after the "needed by" date | S |
| FR-3.7 | Add "Units needed" and "Needed by (date/time)" fields | M |

### FR-4 Real-time Notifications and Escalation

| ID | Requirement | Priority |
|---|---|---|
| FR-4.1 | In-app **notification bell** with unread count (SignalR, live) | M |
| FR-4.2 | Top-ranked donors are notified (in-app + email) when a matching request is created | M |
| FR-4.3 | **Escalation waves:** notify top 5 → if no acceptance in X minutes, notify next 10 → then widen radius | S |
| FR-4.4 | Requester is notified when a donor accepts | M |
| FR-4.5 | SMS notifications | C (needs paid gateway) |

### FR-5 Privacy and Consent

| ID | Requirement | Priority |
|---|---|---|
| FR-5.1 | Donor phone number is **masked** (e.g. `01•••••789`) in all search results | M |
| FR-5.2 | Full contact is revealed to the requester **only after the donor accepts** that request | M |
| FR-5.3 | Exact donor location is never shown publicly; map shows approximate area only | M |
| FR-5.4 | Donor can hide their profile from search entirely | S |

### FR-6 Trust, Safety and Moderation

| ID | Requirement | Priority |
|---|---|---|
| FR-6.1 | Every request page shows a **"Blood is free — never pay anyone"** safety notice | M |
| FR-6.2 | Any user can **report** a suspicious request (reason required) | M |
| FR-6.3 | Admin moderation queue: review, verify (✔ Verified badge) or remove requests | M |
| FR-6.4 | Rate limit: a user cannot create more than N open requests at once | S |
| FR-6.5 | Admin **user management**: activate / deactivate accounts (fixes D5) | M |
| FR-6.6 | Audit log of admin actions | C |

### FR-7 Donor Engagement

| ID | Requirement | Priority |
|---|---|---|
| FR-7.1 | **Reliability score** = accepted ÷ notified (feeds the ranking) | M |
| FR-7.2 | Donation history page for each donor | M |
| FR-7.3 | Badges: First Donation, 3× Lifesaver, 5× Hero, Rare Group Hero | S |
| FR-7.4 | Downloadable **donation certificate** (printable page / PDF) | S |
| FR-7.5 | "You can donate again" notification when cooldown ends | S |
| FR-7.6 | Digital **donor card with QR code** | C |

### FR-8 Recurring Needs (Thalassemia / Chronic Patients)

| ID | Requirement | Priority |
|---|---|---|
| FR-8.1 | Requester can mark a request as **recurring** (every N weeks) | S |
| FR-8.2 | **Donor Circle:** donors pledge to support a recurring patient; system rotates among them respecting cooldowns | C |

### FR-9 Maps and Location

| ID | Requirement | Priority |
|---|---|---|
| FR-9.1 | Google Maps view of nearby available donors (approximate positions) with radius circle | M |
| FR-9.2 | Pick request/hospital location on the map (click to set lat/long) | M |
| FR-9.3 | "Use my current location" button (browser geolocation) | S |

### FR-10 Admin Analytics Dashboard

| ID | Requirement | Priority |
|---|---|---|
| FR-10.1 | KPI cards: total donors, eligible now, open requests, fulfilment rate | M |
| FR-10.2 | Charts: requests by blood group, by district, by urgency; trend over time | M |
| FR-10.3 | **Average time to first donor acceptance** (key performance metric) | M |
| FR-10.4 | Rare blood group availability overview | S |
| FR-10.5 | Export reports to CSV | S |

### FR-11 Modern UX

| ID | Requirement | Priority |
|---|---|---|
| FR-11.1 | Fully responsive, mobile-first layout | M |
| FR-11.2 | Dark mode | S |
| FR-11.3 | Installable web app (PWA) | C |
| FR-11.4 | Bangla / English language switch | C |

### FR-12 Demo Readiness

| ID | Requirement | Priority |
|---|---|---|
| FR-12.1 | Fix fresh-install crash (D1) and machine-specific config (D2) | M |
| FR-12.2 | Demo data seeder: ~50 realistic **fictional** donors across districts, sample requests and history | M |

---

## 6. Smart Matching & Ranking — Specification

### 6.1 Hard filters (donor is excluded if any is true)
1. Blood group is not compatible with the patient's group
2. Days since last donation < cooldown (90 male / 120 female)
3. "Available now" is off, or account is deactivated
4. Donor has no location and is outside the request's district
5. Donor is the requester themself

### 6.2 Weighted score

```
MatchScore = 0.35 × Compatibility
           + 0.30 × Proximity
           + 0.20 × Readiness
           + 0.15 × Reliability          (each factor normalised 0–1)
```

| Factor | Calculation |
|---|---|
| **Compatibility** | Exact group = 1.0 · Compatible = 0.7 · O− donating to a non-O− patient = 0.5 (preserve the universal donor) |
| **Proximity** | `max(0, 1 − distanceKm / radiusKm)` using Haversine. If no coordinates: same thana = 0.8, same district = 0.5 |
| **Readiness** | `min(1, (daysSinceLast − cooldown) / 180)`; never donated = 1.0 |
| **Reliability** | `accepted / notified`; new donor (< 3 notifications) = 0.5 |

### 6.3 Search radius by urgency

| Urgency | Initial radius | Escalated radius |
|---|---|---|
| Normal | 10 km | 25 km |
| Urgent | 15 km | 40 km |
| Critical | 25 km | Whole district + neighbours |

### 6.4 Output
Ranked list (highest score first) with: Match %, explanation chips, masked contact, approximate distance, Notify button.

---

## 7. Data Model Changes (summary)

| Entity | Change |
|---|---|
| `Donor` | + `UserId` (link to account), + `Gender`, + `IsAvailable`, + `IsHidden`; relax 90-day validation (D4) |
| `BloodRequest` | + `RequesterUserId`, + `UnitsNeeded`, + `NeededBy`, + `IsVerified`, + `IsRecurring`/`RecurrenceWeeks`; Status enum + `DonorMatched`, `Cancelled`, `Expired` |
| **New** `DonorResponse` | RequestId, DonorId, Status (Notified / Accepted / Declined / Confirmed), MatchScore, timestamps |
| **New** `Donation` | DonorId, RequestId (optional), Date, Units, Hospital |
| **New** `Notification` | UserId, Title, Message, Link, IsRead, CreatedAt |
| **New** `RequestReport` | RequestId, ReporterUserId, Reason, Status, CreatedAt |
| **New** `Badge` / `DonorBadge` | (Should — only if FR-7.3 is built) |

---

## 8. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Security** | Role-based authorisation on every admin action; anti-forgery tokens on all forms; API keys kept in User Secrets / environment variables, never in Git |
| **Privacy** | Donor phone and exact location never exposed without consent (FR-5); only fictional data in the demo seeder |
| **Performance** | Matching for 1,000 donors returns in < 1 second |
| **Usability** | Request can be created in under 1 minute on a mobile phone |
| **Reliability** | App starts cleanly on an empty database on any PC (FR-12.1) |
| **Maintainability** | Matching logic in a separate, unit-testable service; weights in configuration |
| **Compatibility** | Latest Chrome, Edge, Firefox; mobile browsers |

---

## 9. Out of Scope (Future Work)

- SMS gateway integration (paid provider required)
- Real-time blood bank / hospital inventory integration
- National ID verification
- Native Android / iOS app
- AI/ML demand prediction

---

## 10. Delivery Plan

| Day | Scope |
|---|---|
| **Day 1** | FR-12 (fixes, demo data) · FR-2 eligibility & gender · data model changes (§7) · **FR-1 Smart Matching & Ranking** · FR-3 request lifecycle · FR-5 privacy masking |
| **Day 2** | FR-4 notifications · FR-6 trust & moderation + user management · FR-7.1–7.2 reliability & history · FR-9 Google Maps · FR-10 analytics dashboard · FR-11.1 responsive polish |
| **Day 3 (buffer)** | End-to-end testing, bug fixes, "Should" items only if everything above is stable |

**Code freeze:** end of Day 3. No new features after that.

---

## 11. Assumptions and Decisions

| # | Decision |
|---|---|
| A1 | Maps: **Google Maps JavaScript API** (user-provided key; requires a billing-enabled Google Cloud project even on the free tier). Key stored via .NET User Secrets |
| A2 | Gender field will be added to donors (confirmed) |
| A3 | Cooldown: 90 days male / 120 days female, configurable |
| A4 | Email is the only external notification channel; SMS is future work |
| A5 | All demo data is fictional — no real donor or patient information |

---

## 12. Sources (market research)

- Dhaka Tribune — *Online platforms help save lives*: https://www.dhakatribune.com/science-technology-environment/technology/185787/online-platforms-help-save-lives
- TBS News — *Blood donation goes digital: Lessons from global best practices*: https://www.tbsnews.net/thoughts/blood-donation-goes-digital-lessons-global-best-practices-1164731
- Facebook — *Making it easier to donate blood in Bangladesh*: https://about.fb.com/news/h/facebook-making-it-easier-to-donate-blood-bangladesh
- UTHM (2025) — Blood Bank Volunteer Apps study: https://publisher.uthm.edu.my/periodicals/index.php/aitcs/article/download/16187/6387/120863
- UTHM (2025) — Blood Bank Connect: https://publisher.uthm.edu.my/periodicals/index.php/aitcs/article/download/20427/6856/126727
- Kennesaw State — *Mobile applications for encouraging blood donation: A systematic review and case study*: https://digitalcommons.kennesaw.edu/facpubs/7088
- My Hema Circle — *Why create My Hema Circle*: https://sites.google.com/view/my-hema-circle/why-create-my-hema-circle-mhc
- Acibadem — *Regular transfusions for thalassemia*: https://acibademinternational.com/blog/regular-transfusions-for-thalassemia-how-scheduling-works-and-what-each-visit-involves/

> Note: several sources are news reports from 2018–2022. Verify facts and figures before citing them in the thesis paper.
