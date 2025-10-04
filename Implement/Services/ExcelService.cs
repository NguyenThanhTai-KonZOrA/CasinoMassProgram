using ClosedXML.Excel;
using Common.Enums;
using Implement.EntityModels;
using Implement.Repositories.Interface;
using Implement.Services.Interface;
using Implement.UnitOfWork;
using Implement.ViewModels.Request;
using Implement.ViewModels.Response;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Implement.Services;

public class ExcelService : IExcelService
{
    private readonly ILogger<ExcelService> _logger;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly IImportCellErrorRepository _importCellErrorRepository;
    private readonly IImportRowRepository _importRowRepository;
    private readonly IAwardSettlementRepository _awardSettlementRepository;
    private readonly ITeamRepresentativeRepository _teamRepresentativeRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ITeamRepresentativeMemberRepository _teamRepresentativeMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHostingEnvironment _hostingEnvironment;
    public ExcelService(
        ILogger<ExcelService> logger,
        IImportBatchRepository importBatchRepository,
        IImportCellErrorRepository importCellErrorRepository,
        IImportRowRepository importRowRepository,
        IAwardSettlementRepository awardSettlementRepository,
        ITeamRepresentativeRepository teamRepresentativeRepository,
        ITeamRepresentativeMemberRepository teamRepresentativeMemberRepository,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork, IHostingEnvironment hostingEnvironment
    )
    {
        _logger = logger;
        _importBatchRepository = importBatchRepository;
        _importCellErrorRepository = importCellErrorRepository;
        _importRowRepository = importRowRepository;
        _awardSettlementRepository = awardSettlementRepository;
        _teamRepresentativeRepository = teamRepresentativeRepository;
        _memberRepository = memberRepository;
        _teamRepresentativeMemberRepository = teamRepresentativeMemberRepository;
        _unitOfWork = unitOfWork;
        _hostingEnvironment = hostingEnvironment;
    }

    // Expected headers (ALL required)
    private static readonly string[] RequiredHeaders = new[]
    {
        "SEGMENT",
        "Team Representative",
        "ID",
        "Month",
        "Settlement Doc",
        "No",
        "Member ID",
        "Member name",
        //"Joined date",
        //"Last gaming date",
        //"Eligible (Y/N)",
        "Casino win/(loss)",
        "Award settlement"
    };

    public async Task<ImportSummaryResponse> ImportAndValidateAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        var firstRow = ws.FirstRowUsed();
        var lastRow = ws.LastRowUsed();
        var headerRow = firstRow.RowNumber();
        var startRow = headerRow + 1;

        var headers = ws.Row(headerRow)
            .CellsUsed()
            .ToDictionary<IXLCell, int, string>(
                c => c.Address.ColumnNumber,
                c => c.GetString().Trim()
            );

        // Ensure all headers exist
        var headerNames = headers.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingHeaders = RequiredHeaders.Where(h => !headerNames.Contains(h)).ToList();
        if (missingHeaders.Count > 0)
            throw new InvalidOperationException("Missing required columns: " + string.Join(", ", missingHeaders));

        var batch = new ImportBatch
        {
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            Status = ExcelProcessEnum.Validated.ToString(),
            FileContent = ms.ToArray()
        };

        int total = 0, valid = 0, invalid = 0;

        for (int r = startRow; r <= lastRow.RowNumber(); r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;

            total++;

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers)
            {
                var value = ws.Cell(r, h.Key).GetFormattedString().Trim();
                data[h.Value] = value;
            }

            var errors = ValidateRow(data);
            var isValid = errors.Count == 0;

            if (isValid) valid++; else invalid++;

            var importRow = new ImportRow
            {
                RowNumber = r,
                IsValid = isValid,
                RawJson = JsonSerializer.Serialize(data)
            };

            foreach (var e in errors)
                importRow.Errors.Add(new ImportCellError { Column = e.Column, Message = e.Message });

            batch.Rows.Add(importRow);
        }

        batch.TotalRows = total;
        batch.ValidRows = valid;
        batch.InvalidRows = invalid;

        await _importBatchRepository.AddAsync(batch);
        await _unitOfWork.CompleteAsync();

        return new ImportSummaryResponse
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            UploadedAt = batch.UploadedAt,
            Status = batch.Status,
            TotalRows = total,
            ValidRows = valid,
            InvalidRows = invalid,
            SampleErrors = batch.Rows.Where(r => !r.IsValid).Take(10)
                .Select(r => new RowErrorDto
                {
                    RowNumber = r.RowNumber,
                    Errors = r.Errors.Select(e => new CellErrorData { Column = e.Column, Message = e.Message }).ToList()
                }).ToList()
        };
    }

    public async Task<ImportSummaryResponse> GetBatchSummaryAsync(Guid batchId)
    {
        var batch = await _importBatchRepository.FirstOrDefaultAsync(b => b.Id == batchId, b => b.Rows);
        if (batch is null) throw new Exception("Not Found batch");

        var rows = await _importRowRepository.FindAsync(r => r.BatchId == batch.Id, r => r.Errors);
        batch.Rows = rows.ToList();

        return new ImportSummaryResponse
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            UploadedAt = batch.UploadedAt,
            Status = batch.Status,
            TotalRows = batch.TotalRows,
            ValidRows = batch.ValidRows,
            InvalidRows = batch.InvalidRows,
            SampleErrors = batch.Rows.Where(r => !r.IsValid).Select(r => new RowErrorDto
            {
                RowNumber = r.RowNumber,
                Errors = r.Errors.Select(e => new CellErrorData { Column = e.Column, Message = e.Message }).ToList()
            }).ToList()
        };
    }

    public async Task<ImportDetailsResponse> GetBatchDetailsAsync(Guid batchId)
    {
        var batch = await _importBatchRepository.FirstOrDefaultAsync(b => b.Id == batchId, b => b.Rows);
        if (batch is null) throw new Exception("Not Found batch");

        var rowsWithErrors = await _importRowRepository.FindAsync(r => r.BatchId == batch.Id, r => r.Errors);
        batch.Rows = rowsWithErrors.OrderBy(r => r.RowNumber).ToList();

        var headerSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parsedRows = new List<(int RowNumber, bool IsValid, Dictionary<string, string> Data, List<ImportCellError> Errors)>();

        foreach (var r in batch.Rows)
        {
            var data = string.IsNullOrWhiteSpace(r.RawJson)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : (JsonSerializer.Deserialize<Dictionary<string, string>>(r.RawJson) ?? new(StringComparer.OrdinalIgnoreCase));

            foreach (var k in data.Keys) headerSet.Add(k);
            parsedRows.Add((r.RowNumber, r.IsValid, data, r.Errors.ToList()));
        }

        var preferredOrder = new[]
        {
        "SEGMENT",
        "Team Representative",
        "ID",
        "Month",
        "Settlement Doc",
        "No",
        "Member ID",
        "Member name",
        "Joined date",
        "Last gaming date",
        "Eligible (Y/N)",
        "Casino win/(loss)",
        "Award settlement"
    };

        var headers = preferredOrder.Where(h => headerSet.Contains(h)).ToList();
        headers.AddRange(headerSet.Except(headers, StringComparer.OrdinalIgnoreCase));

        return new ImportDetailsResponse
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            UploadedAt = batch.UploadedAt,
            Status = batch.Status,
            TotalRows = batch.TotalRows,
            ValidRows = batch.ValidRows,
            InvalidRows = batch.InvalidRows,
            Headers = headers,
            Rows = parsedRows.Select(pr => new RowDetailsData
            {
                RowNumber = pr.RowNumber,
                IsValid = pr.IsValid,
                Data = pr.Data,
                Errors = pr.Errors.Select(e => new CellErrorData { Column = e.Column, Message = e.Message }).ToList()
            }).ToList()
        };
    }

    public async Task<ImportDetailsResponse> GetBatchDetailsPagingAsync(Guid batchId, int page = 1, int pageSize = 50)
    {
        var batch = await _importBatchRepository.FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch is null) throw new Exception("Not Found batch");

        // Clamp
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        // Page rows by BatchId, ordered by RowNumber, include Errors
        var paged = await _importRowRepository.FindPagedAsync(
            r => r.BatchId == batchId,
            page,
            pageSize,
            q => q.OrderBy(r => r.RowNumber),
            r => r.Errors
        );

        // Build headers: Required + keys from current page
        var headerSet = new HashSet<string>(RequiredHeaders, StringComparer.OrdinalIgnoreCase);
        var parsedRows = new List<(int RowNumber, bool IsValid, Dictionary<string, string> Data, List<ImportCellError> Errors)>();

        foreach (var r in paged.Items)
        {
            var data = string.IsNullOrWhiteSpace(r.RawJson)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : (JsonSerializer.Deserialize<Dictionary<string, string>>(r.RawJson) ?? new(StringComparer.OrdinalIgnoreCase));

            foreach (var k in data.Keys) headerSet.Add(k);
            parsedRows.Add((r.RowNumber, r.IsValid, data, r.Errors.ToList()));
        }

        var preferredOrder = new[]
        {
        "SEGMENT",
        "Team Representative",
        "ID",
        "Month",
        "Settlement Doc",
        "No",
        "Member ID",
        "Member name",
        "Joined date",
        "Last gaming date",
        "Eligible (Y/N)",
        "Casino win/(loss)",
        "Award settlement"
    };

        var headers = preferredOrder.Where(headerSet.Contains).ToList();
        headers.AddRange(headerSet.Except(headers, StringComparer.OrdinalIgnoreCase));

        return new ImportDetailsResponse
        {
            BatchId = batch.Id,
            FileName = batch.FileName,
            UploadedAt = batch.UploadedAt,
            Status = batch.Status,
            TotalRows = batch.TotalRows,
            ValidRows = batch.ValidRows,
            InvalidRows = batch.InvalidRows,
            Headers = headers,
            Rows = parsedRows.Select(pr => new RowDetailsData
            {
                RowNumber = pr.RowNumber,
                IsValid = pr.IsValid,
                Data = pr.Data,
                Errors = pr.Errors.Select(e => new CellErrorData { Column = e.Column, Message = e.Message }).ToList()
            }).ToList(),

            // Paging
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            HasPrevious = paged.HasPrevious,
            HasNext = paged.HasNext
        };
    }

    public async Task<ApprovedImportResponse> ApprovedImport(Guid batchId)
    {
        var batch = await _importBatchRepository.FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch is null) throw new Exception("Not Found batch");

        if (batch.Rows == null || batch.Rows.Count == 0)
        {
            var rows = await _importRowRepository.FindAsync(r => r.BatchId == batch.Id);
            batch.Rows = rows.ToList();
        }

        if (string.Equals(batch.Status, ExcelProcessEnum.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new Exception("This batch have been 'Approved'.");
        if (!string.Equals(batch.Status, "Validated", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Batch must be in 'Validated' status.");

        var toInsertSettlements = new List<AwardSettlement>();
        var upsertedReps = new Dictionary<string, TeamRepresentative>(StringComparer.OrdinalIgnoreCase);
        var upsertedMembers = new Dictionary<string, Member>(StringComparer.OrdinalIgnoreCase);
        var trmPairs = new HashSet<(Guid RepId, Guid MemberId)>();

        // Preload existing reps and members to reduce DB round-trips
        var existingReps = await _teamRepresentativeRepository.GetAllAsync();
        foreach (var r in existingReps) upsertedReps[r.TeamRepresentativeId] = r;

        var existingMembers = await _memberRepository.GetAllAsync();
        foreach (var m in existingMembers) upsertedMembers[m.MemberCode] = m;

        foreach (var row in batch.Rows.Where(r => r.IsValid))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(row.RawJson) ?? new();
            string Get(string key) => data.TryGetValue(key, out var v) ? v?.Trim() ?? "" : "";

            // Parse required typed values
            if (!TryParseDateOnly(Get("Month"), out var monthStart)) continue;
            if (!TryParseDateOnly(Get("Joined date"), out var joinedDate)) continue;
            if (!TryParseDateOnly(Get("Last gaming date"), out var lastGamingDate)) continue;
            if (!TryParseYesNo(Get("Eligible (Y/N)"), out var eligible)) continue;
            if (!TryParseMoney(Get("Casino win/(loss)"), out var casinoWinLoss)) continue;
            if (!TryParseMoney(Get("Award settlement"), out var awardSettlement)) continue;
            if (!int.TryParse(Get("No"), out var noValue)) continue;

            // Upsert TeamRepresentative by ExternalId = "ID"
            var repExternalId = Get("ID");
            if (!upsertedReps.TryGetValue(repExternalId, out var rep))
            {
                rep = new TeamRepresentative
                {
                    TeamRepresentativeId = repExternalId,
                    TeamRepresentativeName = Get("Team Representative"),
                    Segment = Get("SEGMENT")
                };
                await _teamRepresentativeRepository.AddAsync(rep);
                upsertedReps[repExternalId] = rep;
            }
            else
            {
                // Keep existing values; if empty, fill from current row
                if (string.IsNullOrWhiteSpace(rep.TeamRepresentativeName)) rep.TeamRepresentativeName = Get("Team Representative");
                if (string.IsNullOrWhiteSpace(rep.Segment)) rep.Segment = Get("SEGMENT");
            }

            // Upsert Member by MemberCode = "Member ID"
            var memberCode = Get("Member ID");
            if (!upsertedMembers.TryGetValue(memberCode, out var member))
            {
                member = new Member
                {
                    MemberCode = memberCode,
                    FullName = Get("Member name")
                };
                await _memberRepository.AddAsync(member);
                upsertedMembers[memberCode] = member;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(member.FullName))
                    member.FullName = Get("Member name");
            }

            // Link TR <-> Member in join table (dedupe in-memory)
            trmPairs.Add((rep.Id, member.Id));

            // Per-row settlement
            var monthStartValue = new DateOnly(monthStart.Year, monthStart.Month, 1);

            var settlement = new AwardSettlement
            {
                TeamRepresentative = rep,
                Member = member,
                MonthStart = monthStartValue,
                SettlementDoc = Get("Settlement Doc"),
                No = noValue,
                JoinedDate = joinedDate,
                LastGamingDate = lastGamingDate,
                Eligible = eligible,
                CasinoWinLoss = casinoWinLoss,
                AwardSettlementAmount = awardSettlement
            };
            toInsertSettlements.Add(settlement);
        }

        // Persist all changes in a single SaveChanges
        // Insert join rows (avoid duplicates)
        foreach (var pair in trmPairs)
        {
            var exists = await _teamRepresentativeMemberRepository.AnyAsync(x => x.TeamRepresentativeId == pair.RepId && x.MemberId == pair.MemberId);
            if (!exists)
            {
                await _teamRepresentativeMemberRepository.AddAsync(new TeamRepresentativeMember
                {
                    TeamRepresentativeId = pair.RepId,
                    MemberId = pair.MemberId
                });
            }
        }

        _awardSettlementRepository.AddRange(toInsertSettlements);
        batch.Status = ExcelProcessEnum.Approved.ToString();
        await _unitOfWork.CompleteAsync();

        return new ApprovedImportResponse
        {
            Representatives = upsertedReps.Count,
            Members = upsertedMembers.Count,
            Links = trmPairs.Count,
            SettlementsInserted = toInsertSettlements.Count
        };
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadAnnotatedAsync(Guid batchId)
    {
        var batch = await _importBatchRepository.FirstOrDefaultAsync(b => b.Id == batchId, b => b.Rows);
        if (batch is null) throw new Exception("Batch not found.");
        if (batch.FileContent is null || batch.FileContent.Length == 0)
            throw new Exception("Original file content is not available.");

        var rows = await _importRowRepository.FindAsync(r => r.BatchId == batch.Id, r => r.Errors);
        batch.Rows = rows.ToList();

        using var wb = new XLWorkbook(new MemoryStream(batch.FileContent));
        var ws = wb.Worksheets.First();
        var headerRow = ws.FirstRowUsed();

        var headers = headerRow.Cells().ToDictionary(
            c => c.Address?.ColumnNumber ?? 0,
            c => (c.GetString() ?? string.Empty).Trim()
        );

        var errorMap = batch.Rows
            .Where(r => !r.IsValid)
            .ToDictionary(
                r => r.RowNumber,
                r => r.Errors.Select(e => (e.Column, e.Message)).ToList());

        var lastCol = ws.LastColumnUsed().ColumnNumber();
        var errorsColIdx = lastCol + 1;
        ws.Cell(1, errorsColIdx).Value = "__Errors";

        foreach (var kvp in errorMap)
        {
            var rowIdx = kvp.Key;
            var xlRow = ws.Row(rowIdx);
            if (xlRow.IsEmpty()) continue;

            foreach (var (columnName, _) in kvp.Value)
            {
                var colIdx = headers
                    .Where(h => string.Equals(h.Value, columnName, StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Key)
                    .FirstOrDefault();

                if (colIdx > 0)
                {
                    var cell = ws.Cell(rowIdx, colIdx);
                    cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                    cell.Style.Font.FontColor = XLColor.Black;
                }
            }

            ws.Cell(rowIdx, errorsColIdx).Value = string.Join(" | ", kvp.Value.Select(e => $"{e.Column}: {e.Message}"));
            ws.Cell(rowIdx, errorsColIdx).Style.Fill.BackgroundColor = XLColor.Yellow;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = Path.GetFileNameWithoutExtension(batch.FileName) + "_annotated.xlsx";
        return (ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> GenerateSettlementPaymentReportAsync(GenerateCrpReportRequest generateCrpReport)
    {
        if (generateCrpReport == null) throw new ArgumentNullException(nameof(generateCrpReport));
        if (string.IsNullOrWhiteSpace(generateCrpReport.TeamRepresentativeId))
            throw new ArgumentException("TeamRepresentativeId is required.", nameof(generateCrpReport.TeamRepresentativeId));

        var monthStart = new DateOnly(generateCrpReport.Month.Year, generateCrpReport.Month.Month, 1);

        var allReps = await _teamRepresentativeRepository.GetAllAsync();
        var rep = allReps.FirstOrDefault(r =>
                string.Equals(r.TeamRepresentativeId, generateCrpReport.TeamRepresentativeId, StringComparison.OrdinalIgnoreCase))
            ?? (Guid.TryParse(generateCrpReport.TeamRepresentativeId, out var repGuid)
                ? allReps.FirstOrDefault(r => r.Id == repGuid)
                : null);

        if (rep is null)
            throw new InvalidOperationException($"TeamRepresentative '{generateCrpReport.TeamRepresentativeId}' not found.");

        var settlements = await _awardSettlementRepository.FindAsync(
            s => s.TeamRepresentativeId == rep.Id && s.MonthStart == monthStart,
            s => s.Member!,
            s => s.TeamRepresentative!
        );

        var payments = await _unitOfWork.PaymentTeamRepresentative.GetAllNoTrackingAsync();
        var payment = payments.FirstOrDefault(p => p.TeamRepresentativeId == rep.Id && p.MonthStart == monthStart);
        var awardTotal = payment?.AwardTotal ?? settlements.Sum(s => s.AwardSettlementAmount);

        var templatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "CRP Settlement Sample.xlsx");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template not found. Ensure it’s copied to output: Resources/CRP Settlement Sample.xlsx", templatePath);

        using var wb = new XLWorkbook(templatePath);
        var ws = wb.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Template has no worksheets.");

        ws.Cell("C4").Value = settlements.FirstOrDefault()?.SettlementDoc;
        ws.Cell("E6").Value = rep.TeamRepresentativeName;
        ws.Cell("E7").Value = rep.TeamRepresentativeId;
        ws.Cell("E9").Value = monthStart.ToDateTime(TimeOnly.MinValue);

        var neededHeaders = new[]
        {
            "No.", "Member ID", "Member name", "Joined date", "Last gaming date", "Eligible (Y/N)", "Casino win/(loss)"
        };

        int headerRow = 1;
        Dictionary<string, int> headerCols = new(StringComparer.OrdinalIgnoreCase);

        for (int r = 1; r <= 30; r++)
        {
            var cols = ws.Row(r).CellsUsed().ToDictionary(
                c => c.Address.ColumnNumber,
                c => (c.GetString() ?? string.Empty).Trim()
            );

            if (cols.Values.Any(v => v.Equals("Member ID", StringComparison.OrdinalIgnoreCase)) &&
                cols.Values.Any(v => v.Equals("Member name", StringComparison.OrdinalIgnoreCase)))
            {
                headerRow = r;
                headerCols = cols
                    .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);
                break;
            }
        }

        foreach (var h in neededHeaders)
        {
            if (!headerCols.ContainsKey(h))
                throw new InvalidOperationException($"Template is missing required header: '{h}'.");
        }

        void TrySet(string headerText, string value)
        {
            var cell = ws.CellsUsed(c => string.Equals(c.GetString().Trim(), headerText, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (cell != null) cell.CellRight().Value = value;
        }

        TrySet("Team Representative", $"{rep.TeamRepresentativeName} ({rep.TeamRepresentativeId})");
        TrySet("Month", $"{monthStart:yyyy-MM}");

        var firstDataRow = headerRow + 1;
        var lastUsedRow = ws.LastRowUsed()?.RowNumber() ?? firstDataRow - 1;
        if (lastUsedRow >= firstDataRow)
            ws.Rows(firstDataRow, lastUsedRow).Clear(XLClearOptions.Contents);

        int idx(string header) => headerCols[header];

        var row = firstDataRow;
        foreach (var (s, no) in settlements.Select((s, i) => (s, i + 1)))
        {
            ws.Cell(row, idx("No.")).Value = no;
            ws.Cell(row, idx("Member ID")).Value = s.Member?.MemberCode ?? string.Empty;
            ws.Cell(row, idx("Member name")).Value = s.Member?.FullName ?? string.Empty;

            ws.Cell(row, idx("Joined date")).Value = s.JoinedDate.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, idx("Joined date")).Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Cell(row, idx("Last gaming date")).Value = s.LastGamingDate.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, idx("Last gaming date")).Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Cell(row, idx("Eligible (Y/N)")).Value = s.Eligible ? "Y" : "N";

            ws.Cell(row, idx("Casino win/(loss)")).Value = (double)s.CasinoWinLoss;
            ws.Cell(row, idx("Casino win/(loss)")).Style.NumberFormat.Format = "#,##0.00;(#,##0.00)";

            row++;
        }

        var labelCol = headerCols.ContainsKey("Eligible (Y/N)") ? idx("Eligible (Y/N)") : idx("Last gaming date");
        var numberCol = idx("Casino win/(loss)");
        var totalWinLoss = settlements.Sum(s => s.CasinoWinLoss);

        int totalRow = row;
        ws.Cell(row, labelCol).Value = "Total:";
        ws.Cell(row, labelCol).Style.Font.Bold = true;

        ws.Cell(row, numberCol).Value = (double)totalWinLoss;
        ws.Cell(row, numberCol).Style.NumberFormat.Format = "#,##0.00;(#,##0.00)";
        ws.Cell(row, numberCol).Style.Font.Bold = true;

        ws.Row(row).Style.Border.TopBorder = XLBorderStyleValues.Thin;

        row++;

        int awardRow = row;
        ws.Cell(row, labelCol).Value = "Award settlement:";
        ws.Cell(row, labelCol).Style.Font.Bold = true;

        ws.Cell(row, numberCol).Value = (double)awardTotal;
        ws.Cell(row, numberCol).Style.NumberFormat.Format = "#,##0.00;(#,##0.00)";
        ws.Cell(row, numberCol).Style.Font.Bold = true;

        // Draw full border (header + data + summary)
        var minCol = neededHeaders.Min(h => idx(h));
        var maxCol = neededHeaders.Max(h => idx(h));
        var tableRange = ws.Range(headerRow, minCol, awardRow, maxCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Leave 4 blank rows then signature headers
        int signatureHeaderRow = awardRow + 5; // 4 blank rows after award row
        int signatureValueRow = signatureHeaderRow + 5; // 4 blank rows for signing before values
        var labelColSign = headerCols.ContainsKey("Member ID") ? idx("Member ID") : idx("Member name");
        // Choose three columns for signatures (left, middle, right)
        int paidCol = labelColSign;
        int confirmCol = (minCol + maxCol) / 2;
        int receivedCol = maxCol;

        ws.Cell(signatureHeaderRow, paidCol).Value = "Paid by";
        ws.Cell(signatureHeaderRow, confirmCol).Value = "Confirmed by";
        ws.Cell(signatureHeaderRow, receivedCol).Value = "Received by";

        ws.Range(signatureHeaderRow, paidCol, signatureHeaderRow, receivedCol).Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Values after 4 more rows
        ws.Cell(signatureValueRow, paidCol).Value = "Cage";
        ws.Cell(signatureValueRow, confirmCol).Value = "Casino Marketting";
        ws.Cell(signatureValueRow, receivedCol).Value = "0";

        ws.Range(signatureValueRow, paidCol, signatureValueRow, receivedCol).Style
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Font.SetBold();

        // Optional signature line (bottom border) above the values
        ws.Cell(signatureValueRow - 1, paidCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(signatureValueRow - 1, confirmCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Cell(signatureValueRow - 1, receivedCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var fileName = $"CRP_Settlement_{rep.TeamRepresentativeId}_{monthStart:yyyyMM}.xlsx";
        return (ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    public async Task<List<ImportBatch>> ListBatchesAsync()
    {
        var batch = await _importBatchRepository.GetAllNoTrackingAsync();
        return batch.ToList();
    }

    #region Validation and mapping values
    private static List<(string Column, string Message)> ValidateRow(Dictionary<string, string> data)
    {
        var errors = new List<(string, string)>();

        // All required
        foreach (var col in RequiredHeaders)
        {
            if (!data.TryGetValue(col, out var v) || string.IsNullOrWhiteSpace(v))
                errors.Add((col, "Required"));
        }

        // Stop early if missing requireds
        if (errors.Count > 0) return errors;

        // Month
        if (!TryParseMonth(data["Month"], out _))
            errors.Add(("Month", "Invalid month"));

        // No
        if (!int.TryParse(data["No"], out _))
            errors.Add(("No", "Invalid integer"));

        // Dates
        if (!TryParseDateOnly(data["Joined date"], out _))
            errors.Add(("Joined date", "Invalid date"));
        if (!TryParseDateOnly(data["Last gaming date"], out _))
            errors.Add(("Last gaming date", "Invalid date"));

        // Eligible
        var check = TryParseYesNo(data["Eligible (Y/N)"], out _);

        if (!TryParseYesNo(data["Eligible (Y/N)"], out _))
            errors.Add(("Eligible (Y/N)", "Must be Y or N"));

        // Money
        if (!TryParseMoney(data["Casino win/(loss)"], out _))
            errors.Add(("Casino win/(loss)", "Invalid number"));
        if (!TryParseMoney(data["Award settlement"], out _))
            errors.Add(("Award settlement", "Invalid number"));

        // Note: ID duplicates are allowed (grouped), no duplicate check here.
        return errors;
    }

    private static bool TryParseDateOnly(string value, out DateOnly date)
    {
        // Empty => default today
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
            return true;
        }

        var v = value.Trim();

        // Explicit formats: dd/MM/yyyy and MM/dd/yyyy (single-digit variants allowed)
        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy" };
        if (DateTime.TryParseExact(v, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            date = DateOnly.FromDateTime(exact);
            return true;
        }

        // Excel OADate (serial number)
        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var oa))
        {
            try
            {
                date = DateOnly.FromDateTime(DateTime.FromOADate(oa));
                return true;
            }
            catch
            {
                // ignore
            }
        }

        // Fallback broad parse
        if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseMonth(string value, out DateOnly monthStart)
    {
        monthStart = default;
        var v = value.Trim();

        if (DateTime.TryParse(v, out var dt))
        {
            monthStart = new DateOnly(dt.Year, dt.Month, 1);
            return true;
        }

        var m = Regex.Match(v, @"^(?<y>\d{4})-(?<m>\d{1,2})$");
        if (m.Success &&
            int.TryParse(m.Groups["y"].Value, out var y) &&
            int.TryParse(m.Groups["m"].Value, out var mo) &&
            mo is >= 1 and <= 12)
        {
            monthStart = new DateOnly(y, mo, 1);
            return true;
        }

        m = Regex.Match(v, @"^(?<m>\d{1,2})/(?<y>\d{4})$");
        if (m.Success &&
            int.TryParse(m.Groups["y"].Value, out y) &&
            int.TryParse(m.Groups["m"].Value, out mo) &&
            mo is >= 1 and <= 12)
        {
            monthStart = new DateOnly(y, mo, 1);
            return true;
        }

        return false;
    }

    private static bool TryParseYesNo(string value, out bool yes)
    {
        // Empty => default N
        if (string.IsNullOrWhiteSpace(value))
        {
            yes = false;
            return true;
        }

        var v = value.Trim().ToUpperInvariant();
        if (v == "Y") { yes = true; return true; }
        if (v == "N") { yes = false; return true; }

        yes = false;
        return false;
    }

    private static bool TryParseMoney(string value, out decimal number)
    {
        number = 0m;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var v = value.Trim();
        var negative = v.StartsWith("(") && v.EndsWith(")");
        v = v.Trim('(', ')');

        v = Regex.Replace(v, @"[^\d\.,\-]", "");
        v = v.Replace(",", "");

        if (v == "-")
        {
            v = "0";
            return true;
        }

        if (decimal.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
        {
            number = negative ? -d : d;
            return true;
        }
        return false;
    }
    #endregion
}