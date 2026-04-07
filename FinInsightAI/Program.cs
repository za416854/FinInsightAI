using Microsoft.AspNetCore.Mvc;
using Mscc.GenerativeAI;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI.Types;
using Dapper; // 引用 Gemini SDK



var builder = WebApplication.CreateBuilder(args);
// --- 1. 註冊服務 (Dependency Injection) ---
builder.Services.AddOpenApi();
// 註冊 GeminiService，這樣 Controller 或 Minimal API 就能直接使用它
builder.Services.AddScoped<GeminiService>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


// --- 3. 建立 AI 問答 Endpoint ---
app.MapGet("/ask", async (string prompt, GeminiService gemini, IConfiguration configuration) =>
{
    // 1. AI 生成 SQL
    var rawSql = await gemini.GenerateSql(prompt);

    // 2. 關鍵修正：清理掉可能存在的 Markdown 標籤與換行
    // 這行會去掉 ```sql, ``` 以及前後空白
    var sql = rawSql.Replace("```sql", "").Replace("```", "").Trim();
     
    string connString = configuration["ConnectionStrings:FinInsightDb"] ?? throw new ArgumentNullException("找不到資料庫連線字串設定！");

    using var conn = new Npgsql.NpgsqlConnection(connString);

    try
    {
        // 使用清理後的 sql 變數
        var filteredData = (await conn.QueryAsync<LoanRecord>(sql)).ToList();

        var summary = await gemini.GenerateSummary(prompt, filteredData);

        return Results.Ok(new
        {
            Question = prompt,
            Sql = sql,
            Data = filteredData,
            AIAnalysis = summary
        });
    }
    catch (Exception ex)
    {
        // 增加一個簡單的錯誤處理，幫你抓出到底是哪句 SQL 壞掉
        return Results.BadRequest(new { error = ex.Message, brokenSql = sql });
    }

})
.WithName("AskGemini");

app.Run();



// --- 4. 定義 GeminiService 類別 ---

public class GeminiService
{
    // ⚠️ 建議實務上放入 appsettings.json，並使用 IConfiguration 注入來讀取
    private readonly string _apiKey;
    private readonly IConfiguration _configuration; // 加入這個欄位

    // 在建構子注入 IConfiguration
    public GeminiService(IConfiguration configuration)
    {
        _configuration = configuration;
        // 從 appsettings.json 的 Gemini:ApiKey 路徑讀取數值
        _apiKey = _configuration["Gemini:ApiKey"]
                  ?? throw new ArgumentNullException("找不到 Gemini API Key 設定！");
    }

    private readonly string _schemaContext = @"
你是一個 PostgreSQL 專家。資料表名稱為 loans。
欄位分別為：id, customer_name, amount, status, loan_date。
請注意：
1. 所有的 SQL 語法請使用小寫。
2. 請僅根據使用者問題回傳標準 SQL 語法。
";



    public async Task<string> GenerateSql(string userQuestion)
    {
        // 初始化 Gemini
        var googleAi = new GoogleAI(_apiKey);
        var model = googleAi.GenerativeModel(Model.Gemini25Flash); // 使用快速且免費額度高的 Flash 模型

        // 合併 Prompt
        var fullPrompt = $"{_schemaContext}\n使用者問題:{userQuestion}";

        // 呼叫 AI
        var response = await model.GenerateContent(fullPrompt);

        return response.Text ?? "無法生成 SQL";
    }
    public async Task<string> GenerateSummary(string question, object data)
    {
        var googleAi = new GoogleAI(_apiKey);
        var model = googleAi.GenerativeModel(Model.Gemini25Flash);

        // 這裡就是「提供上下文」給 AI
        var finalPrompt = $@"
        使用者問了這個問題：'{question}'
        我們從資料庫查到了以下數據：{System.Text.Json.JsonSerializer.Serialize(data)}
        
        請以「專業金融分析師」的口吻，針對這份數據回答使用者的問題。
        若數據為空，請客氣地告知查無資料。
        回答請控制在 100 字以內，並強調關鍵數據。";

        var response = await model.GenerateContent(finalPrompt);
        return response.Text ?? "分析失敗";
    }
}



// 定義資料模型
// 修改後的資料模型
public class LoanRecord
{
    public int id { get; init; }
    public string customer_name { get; init; } = string.Empty;
    public decimal amount { get; init; }
    public string status { get; init; } = string.Empty;
    public DateOnly loan_date { get; init; }
}