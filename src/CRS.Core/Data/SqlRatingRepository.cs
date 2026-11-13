using System.Data;
using System.Globalization;
using CRS.Core.Models;

namespace CRS.Core.Data;

public sealed class SqlRatingRepository(ISqlExecutor sql) : IRatingRepository
{
    private static Dictionary<string, object?> Args(params (string Name, object? Value)[] args) => args.ToDictionary(a => a.Name, a => a.Value);
    private static string Text(DataRow row, string column) => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
        ? Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? "" : "";
    private static long Number(DataRow row, string column) => long.TryParse(Text(row, column), out var n) ? n : 0;
    private static Guid? Id(DataRow row, string column) => Guid.TryParse(Text(row, column), out var id) ? id : null;
    private static DateTime? Date(DataRow row, string column) => row.Table.Columns.Contains(column) && row[column] is DateTime d ? d : null;

    public async Task<UserProfile?> GetUserAsync(Guid id, CancellationToken ct)
    {
        var data = await sql.QueryAsync(new("SELECT TOP (1) ID, Extension, DisplayName, Mail FROM dbo.ADUser WHERE ID = @UserID",
            false, Args(("@UserID", id)), true), ct);
        return data.Rows.Count == 0 ? null : new(id, Text(data.Rows[0], "Extension").Trim(), Text(data.Rows[0], "DisplayName"), Text(data.Rows[0], "Mail"));
    }
    public async Task<Lookups> GetLookupsAsync(CancellationToken ct)
    {
        async Task<IReadOnlyList<Lookup>> Load(string type, string id, string name)
        {
            var data = await sql.QueryAsync(new("CSP_GetDropdownList", true, Args(("@DropdownType", type))), ct);
            return data.Rows.Cast<DataRow>().Select(r => new Lookup(Number(r, id), Text(r, name))).ToArray();
        }
        return new(await Load("Problems", "ProblemsID", "Problems"), await Load("Pharmacy", "PharmacyID", "Pharmacy"),
            await Load("HelpDeskStatus", "HelpDeskStatusID", "HelpDeskStatus"), await Load("HelpDeskReason", "HelpDeskReasonID", "HelpDeskReason"));
    }
    public async Task<IReadOnlyList<CallRating>> GetRatingsAsync(long? statusId, CancellationToken ct)
    {
        var data = await sql.QueryAsync(new("CSP_GetCallRating", true, Args(("@HelpDeskStatusFilter", statusId))), ct);
        return data.Rows.Cast<DataRow>().Select(Map).ToArray();
    }
    public async Task<CallRating?> FindAsync(long id, CancellationToken ct)
    {
        var data = await sql.QueryAsync(new("""
            SELECT cr.*, u.DisplayName, u.Mail, s.HelpDeskStatus, r.HelpDeskReason, p.Pharmacy
            FROM dbo.CallRating cr
            LEFT JOIN ADUsers.dbo.ADUser u ON u.ID = cr.UserID
            LEFT JOIN dbo.Lookup_HelpDeskStatus s ON s.HelpDeskStatusID = cr.HelpDeskStatusID
            LEFT JOIN dbo.Lookup_HelpDeskReason r ON r.HelpDeskReasonID = cr.HelpDeskReasonID
            LEFT JOIN dbo.Lookup_Pharmacy p ON p.PharmacyID = cr.PharmacyID
            WHERE cr.CallRatingID = @Id
            """, false, Args(("@Id", id))), ct);
        return data.Rows.Count == 0 ? null : Map(data.Rows[0]);
    }
    public async Task<bool> AddAsync(CallRating r, CancellationToken ct)
    {
        // The supplied schema has no unique constraint. Serialize check-and-insert on SQL Server,
        // including concurrent submissions from different web instances, without changing the schema.
        var data = await sql.QueryAsync(new("""
            SET XACT_ABORT ON;
            BEGIN TRY
              BEGIN TRANSACTION;
              IF EXISTS (SELECT 1 FROM dbo.CallRating WITH (UPDLOCK, HOLDLOCK)
                         WHERE UserID = @UserID AND call_id = @call_id AND CallDateTime = @CallDateTime)
                BEGIN COMMIT; SELECT CAST(0 AS bit) AS Saved; RETURN; END;
              EXEC dbo.CSP_AddCallRating @UserID=@UserID, @CaptureDate=@CaptureDate,
                @Ext=@Ext, @CallDate=@CallDate, @CallTime=@CallTime, @NumberPhoned=@NumberPhoned,
                @Problems=@Problems, @OtherProblem=@OtherProblem, @CallDateTime=@CallDateTime,
                @ClientIP=@ClientIP, @HostIP=@HostIP, @call_id=@call_id, @PharmacyID=@PharmacyID;
              COMMIT;
              SELECT CAST(1 AS bit) AS Saved;
            END TRY
            BEGIN CATCH
              IF @@TRANCOUNT > 0 ROLLBACK;
              THROW;
            END CATCH
            """, false, Args(("@UserID", r.UserId), ("@CaptureDate", r.CaptureDate), ("@Ext", r.Extension),
            ("@CallDate", r.CallDateTime?.ToString("yyyy-MM-dd")), ("@CallTime", r.CallDateTime?.ToString("HH:mm:ss")),
            ("@NumberPhoned", r.NumberPhoned), ("@Problems", r.Problems), ("@OtherProblem", r.OtherProblem),
            ("@CallDateTime", r.CallDateTime), ("@ClientIP", r.ClientIP), ("@HostIP", r.HostIP),
            ("@call_id", r.RecordingKey), ("@PharmacyID", r.PharmacyId))), ct);
        return data.Rows.Count > 0 && Convert.ToBoolean(data.Rows[0]["Saved"]);
    }
    public async Task<bool> UpdateAsync(long id, Guid userId, DateTime at, UpdateInput input, CancellationToken ct)
    {
        var data = await sql.QueryAsync(new("""
            SET XACT_ABORT ON;
            BEGIN TRY
              BEGIN TRANSACTION;
              IF NOT EXISTS (SELECT 1 FROM dbo.CallRating WITH (UPDLOCK, HOLDLOCK) WHERE CallRatingID = @CallRatingID)
                BEGIN COMMIT; SELECT CAST(0 AS bit) AS Saved; RETURN; END;
              EXEC dbo.CSP_ITUpdate @CallRatingID=@CallRatingID, @ITUpdateByUserID=@ITUpdateByUserID,
                @ITUpdateDate=@ITUpdateDate, @HelpDeskStatusID=@HelpDeskStatusID,
                @HelpDeskReasonID=@HelpDeskReasonID, @ITNote=@ITNote;
              COMMIT;
              SELECT CAST(1 AS bit) AS Saved;
            END TRY
            BEGIN CATCH
              IF @@TRANCOUNT > 0 ROLLBACK;
              THROW;
            END CATCH
            """, false, Args(("@CallRatingID", id), ("@ITUpdateByUserID", userId), ("@ITUpdateDate", at),
            ("@HelpDeskStatusID", input.StatusId), ("@HelpDeskReasonID", input.ReasonId), ("@ITNote", input.Note?.Trim()))), ct);
        return data.Rows.Count > 0 && Convert.ToBoolean(data.Rows[0]["Saved"]);
    }
    private static CallRating Map(DataRow r) => new()
    {
        Id = Number(r, "CallRatingID"), UserId = Id(r, "UserID") ?? Guid.Empty,
        DisplayName = Text(r, "DisplayName"), Email = Text(r, "Mail"), CaptureDate = Date(r, "CaptureDate") ?? DateTime.MinValue,
        Extension = Text(r, "Ext"), CallDateTime = Date(r, "CallDateTime"),
        NumberPhoned = Text(r, r.Table.Columns.Contains("NumberPhoned") ? "NumberPhoned" : "Number Dialed"),
        Problems = Text(r, "Problems"), OtherProblem = Text(r, "OtherProblem"), RecordingKey = Text(r, "call_id"),
        ClientIP = Text(r, "ClientIP"), HostIP = Text(r, "HostIP"), PharmacyId = Number(r, "PharmacyID"),
        Pharmacy = Text(r, "Pharmacy"), StatusId = Number(r, "HelpDeskStatusID"), Status = Text(r, "HelpDeskStatus"),
        ReasonId = Number(r, "HelpDeskReasonID"), Reason = Text(r, "HelpDeskReason"), ITNote = Text(r, "ITNote"),
        UpdatedBy = Id(r, "ITUpdateByUserID"), UpdatedAt = Date(r, "ITUpdateDate")
    };
}
