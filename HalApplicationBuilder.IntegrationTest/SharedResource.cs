using Microsoft.AspNetCore.Http;
using Microsoft.Build.Evaluation;
using Microsoft.Data.Sqlite;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HalApplicationBuilder.IntegrationTest {

    /// <summary>
    /// テスト全体で共有するリソース
    /// </summary>
    [SetUpFixture]
    public class SharedResource {

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static HalappProject Project { get; private set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        #region SETUP
        [OneTimeSetUp]
        public void SetUp() {
            // 出力が文字化けするので
            Console.OutputEncoding = Encoding.UTF8;

            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            const string DIR_NAME = "自動テストで作成されたプロジェクト";
            var logger = new TestContextLogger();
            var dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", DIR_NAME));
            Project = Directory.Exists(dir)
                ? HalappProject.Open(dir, logger)
                : HalappProject.Create(dir, DIR_NAME, true, log: logger);
        }

        [OneTimeTearDown]
        public void TearDown() {

        }
        #endregion SETUP
    }

    /// <summary>
    /// テスト用の糖衣構文
    /// </summary>
    public static class HalappProjectExtension {

        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Get(this HalappProject project, string path, Dictionary<string, string>? parameters = null) {
            var query = parameters == null
                ? string.Empty
                : $"?{await new FormUrlEncodedContent(parameters).ReadAsStringAsync()}";
            var uri = new Uri(project.Debugger.GetDebugUrl(), path + query);

            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }
        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <param name="body">リクエストボディ</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Post(this HalappProject project, string path, object body) {
            var uri = new Uri(project.Debugger.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }
        /// <summary>
        /// テスト用プロジェクトにHTTPリクエストを送信し、結果を受け取ります。
        /// </summary>
        /// <param name="path">URLのうちドメインより後ろの部分</param>
        /// <returns>HTTPレスポンス</returns>
        public static async Task<HttpResponseMessage> Delete(this HalappProject project, string path) {
            var uri = new Uri(project.Debugger.GetDebugUrl(), path);
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);

            using var client = new HttpClient();
            return await client.SendAsync(message);
        }

        /// <summary>
        /// テスト用データベースにSELECT文を発行します。
        /// </summary>
        public static IEnumerable<SqliteDataReader> ExecSql(this HalappProject project, string sql) {
            var dataSource = Path.GetFullPath(Path.Combine(project.ProjectRoot, $"bin/Debug/debug.sqlite3")).Replace("\\", "/");
            var connStr = new SqliteConnectionStringBuilder();
            connStr.DataSource = dataSource;
            connStr.Pooling = false;
            using var conn = new SqliteConnection(connStr.ToString());
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                yield return reader;
            }
        }

        /// <summary>
        /// 実行中のテスト用プロジェクトをWebから操作する機構を作成します。
        /// </summary>
        public static IWebDriver CreateWebDriver(this HalappProject project) {
            var exeDir = Assembly.GetExecutingAssembly().Location;
            var driver = new ChromeDriver(exeDir);

            // トップページに移動する
            var root = SharedResource.Project.Debugger.GetDebuggingClientUrl();
            driver.Navigate().GoToUrl(root);

            return driver;
        }
    }
}
