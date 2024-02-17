using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Reflection;
using System.Text;

namespace Nijo.IntegrationTest {
    /// <summary>
    /// 自動テストで作成されたプロジェクト。テスト全体で共有する
    /// </summary>
    [SetUpFixture]
    public class TestProject {

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public static GeneratedProject Current { get; private set; }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        #region SETUP
        [OneTimeSetUp]
        public void SetUp() {
            var serviceProvider = Util.ConfigureServices();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            // 出力が文字化けするので
            Console.OutputEncoding = Encoding.UTF8;

            // 依存先パッケージのインストールにかかる時間とデータ量を削減するために全テストで1つのディレクトリを共有する
            const string DIR_NAME = "自動テストで作成されたプロジェクト";
            var dir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", DIR_NAME));
            Current = Directory.Exists(dir)
                ? GeneratedProject.Open(dir, serviceProvider, logger)
                : GeneratedProject.Create(dir, DIR_NAME, true, serviceProvider, log: logger);
        }

        [OneTimeTearDown]
        public void TearDown() {

        }
        #endregion SETUP
    }
}
