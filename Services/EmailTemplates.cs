using System.Net;
using BloodDonationManagement.Models;

namespace BloodDonationManagement.Services;

public static class EmailTemplates
{
    public static string RequestAlert(BloodRequest r, double matchScore, string? link)
    {
        var color = r.Urgency == UrgencyLevel.Critical ? "#dc3545" : r.Urgency == UrgencyLevel.Urgent ? "#fd7e14" : "#198754";
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        return $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;border:1px solid #eee;border-radius:8px;overflow:hidden">
              <div style="background:{color};color:white;padding:20px 24px">
                <h2 style="margin:0">🩸 {r.Urgency} blood request — {E(r.BloodGroup)}</h2>
                <div style="opacity:.9">You are a {matchScore:0}% match</div>
              </div>
              <div style="padding:24px">
                <table style="width:100%;border-collapse:collapse">
                  <tr><td style="padding:6px 0;color:#666">Hospital</td><td><b>{E(r.HospitalName ?? "Not specified")}</b></td></tr>
                  <tr><td style="padding:6px 0;color:#666">Units needed</td><td>{r.UnitsNeeded}</td></tr>
                  <tr><td style="padding:6px 0;color:#666">Needed by</td><td>{(r.NeededBy.HasValue ? r.NeededBy.Value.ToString("dd MMM yyyy, hh:mm tt") : "As soon as possible")}</td></tr>
                </table>
                <p style="margin:20px 0">
                  <a href="{E(link)}" style="display:inline-block;background:#dc3545;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold">
                    View request &amp; respond
                  </a>
                </p>
                <p style="color:#b02a37;font-size:13px">⚠ Blood donation is free. Never pay or accept money for blood.</p>
              </div>
              <div style="background:#f8f9fa;padding:12px 24px;color:#999;font-size:12px">BloodConnect BD — Saving lives across Bangladesh</div>
            </div>
            """;
    }
}
