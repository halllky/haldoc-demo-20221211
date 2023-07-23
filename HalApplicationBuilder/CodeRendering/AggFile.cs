// ------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン: 17.0.0.0
//  
//     このファイルへの変更は、正しくない動作の原因になる可能性があり、
//     コードが再生成されると失われます。
// </auto-generated>
// ------------------------------------------------------------------------------
namespace HalApplicationBuilder.CodeRendering
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using HalApplicationBuilder.Core;
    using HalApplicationBuilder.CodeRendering.Util;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class AggFile : AggFileBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。\r" +
                    "\n#pragma warning disable CS8618 // null 非許容の変数には、コンストラクターの終了時に null 以外の値が入っていなけれ" +
                    "ばなりません\r\n#pragma warning disable IDE1006 // 命名スタイル\r\n\r\n#region データ新規作成\r\nnamespace " +
                    "");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using Microsoft.AspNetCore.Mvc;\r\n    using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(";\r\n\r\n    [ApiController]\r\n    [Route(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.SUBDOMAIN));
            this.Write("/[controller]\")]\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write(" : ControllerBase {\r\n        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write("(ILogger<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write("> logger, ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" dbContext) {\r\n            _logger = logger;\r\n            _dbContext = dbContext;" +
                    "\r\n        }\r\n        private readonly ILogger<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write("> _logger;\r\n        private readonly ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" _dbContext;\r\n\r\n        [HttpPost(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.CREATE_ACTION_NAME));
            this.Write("\")]\r\n        public virtual IActionResult Create([FromBody] ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregateInstance.Item.ClassName));
            this.Write(" param) {\r\n            // TODO\r\n            throw new NotImplementedException();\r" +
                    "\n        }\r\n    }\r\n}\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregateInstance.Item.ClassName));
            this.Write(" {\r\n        /// <summary>\r\n        /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("のデータ1件の内容をデータベースに保存する形に変換します。\r\n        /// </summary>\r\n        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(_dbEntity.Item.ClassName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstance.TO_DB_ENTITY_METHOD_NAME));
            this.Write("() {\r\n");
 PushIndent("            "); 
 ToDbEntity(); 
 PopIndent(); 
            this.Write("        }\r\n    }\r\n}\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n    using Microsoft.EntityFrameworkCore;\r\n  " +
                    "  using Microsoft.EntityFrameworkCore.Infrastructure;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" {\r\n    }\r\n}\r\n#endregion データ新規作成\r\n\r\n\r\n#region 一覧検索\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using Microsoft.AspNetCore.Mvc;\r\n    using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(";\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write(" {\r\n        [HttpGet(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.SEARCH_ACTION_NAME));
            this.Write("\")]\r\n        public virtual IActionResult Search([FromQuery] string param) {\r\n   " +
                    "         var json = System.Web.HttpUtility.UrlDecode(param);\r\n            var co" +
                    "ndition = string.IsNullOrWhiteSpace(json)\r\n                ? new ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.ArgType));
            this.Write("()\r\n                : System.Text.Json.JsonSerializer.Deserialize<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.ArgType));
            this.Write(">(json)!;\r\n            var searchResult = _dbContext\r\n                .");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.MethodName));
            this.Write("(condition)\r\n                .AsEnumerable();\r\n            return this.JsonConten" +
                    "t(searchResult);\r\n        }\r\n        [HttpGet(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.KEYWORDSEARCH_ACTION_NAME));
            this.Write("\")]\r\n        public virtual IActionResult SearchByKeyword([FromQuery] string keyw" +
                    "ord) {\r\n            // TODO\r\n            throw new NotImplementedException();\r\n " +
                    "       }\r\n    }\r\n}\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n    using Microsoft.EntityFrameworkCore;\r\n  " +
                    "  using Microsoft.EntityFrameworkCore.Infrastructure;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" {\r\n        /// <summary>\r\n        /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("の一覧検索を行います。\r\n        /// </summary>\r\n        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.ReturnType));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.MethodName));
            this.Write("(");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.ArgType));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.PARAM));
            this.Write(") {\r\n            var ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.QUERY));
            this.Write(" = this.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.DbSetName));
            this.Write(".Select(");
            this.Write(this.ToStringHelper.ToStringWithCulture(E));
            this.Write(" => new ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.ReturnItemType));
            this.Write(" {\r\n");
 foreach (var line in _search.SelectClause()) { 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
            this.Write("            });\r\n\r\n");
 foreach (var line in _search.WhereClause()) { 
            this.Write("            ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
            this.Write("\r\n            if (");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.PARAM));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.PAGE));
            this.Write(" != null) {\r\n                const int PAGE_SIZE = 20;\r\n                var skip " +
                    "= ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.PARAM));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.PAGE));
            this.Write(".Value * PAGE_SIZE;\r\n                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.QUERY));
            this.Write(" = ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.QUERY));
            this.Write(".Skip(skip).Take(PAGE_SIZE);\r\n            }\r\n\r\n            foreach (var item in ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchMethod.QUERY));
            this.Write(") {\r\n                item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_KEY_PROP_NAME));
            this.Write(" = ");
            this.Write(this.ToStringHelper.ToStringWithCulture(InstanceKey.CLASS_NAME));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(InstanceKey.CREATE));
            this.Write("(new object?[] {\r\n");
 foreach (var key in _search.EnumerateKeys()) { 
            this.Write("                    item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(key));
            this.Write(",\r\n");
 } 
            this.Write("                }).ToString();\r\n\r\n");
 if (_search.GetInstanceNamePropName() == null) { 
            this.Write("                // 表示名に使用するプロパティが定義されていないため、キーを表示名に使用します。\r\n                item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_NAME_PROP_NAME));
            this.Write(" = item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_KEY_PROP_NAME));
            this.Write(";\r\n");
 } else { 
            this.Write("                item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.INSTANCE_NAME_PROP_NAME));
            this.Write(" = item.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_search.GetInstanceNamePropName()));
            this.Write(";\r\n");
 } 
            this.Write("\r\n                yield return item;\r\n            }\r\n        }\r\n    }\r\n}\r\n#endreg" +
                    "ion 一覧検索\r\n\r\n\r\n#region 詳細検索\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using Microsoft.AspNetCore.Mvc;\r\n    using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(";\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write(" {\r\n        [HttpGet(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.FIND_ACTION_NAME));
            this.Write("/{instanceKey}\")]\r\n        public virtual IActionResult Find(string instanceKey) " +
                    "{\r\n            var instance = _dbContext.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_find.MethodName));
            this.Write("(instanceKey);\r\n            if (instance == null) {\r\n                return NotFo" +
                    "und();\r\n            } else {\r\n                return this.JsonContent(instance);" +
                    "\r\n            }\r\n        }\r\n    }\r\n}\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n    using Microsoft.EntityFrameworkCore;\r\n  " +
                    "  using Microsoft.EntityFrameworkCore.Infrastructure;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" {\r\n        /// <summary>\r\n        /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("のキー情報から対象データの詳細を検索して返します。\r\n        /// </summary>\r\n        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_find.ReturnType));
            this.Write("? ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_find.MethodName));
            this.Write("(string serializedInstanceKey) {\r\n            if (!");
            this.Write(this.ToStringHelper.ToStringWithCulture(InstanceKey.CLASS_NAME));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(InstanceKey.TRY_PARSE));
            this.Write("(serializedInstanceKey, out var instanceKey)) {\r\n                return null;\r\n  " +
                    "          }\r\n            var entity = this.");
            this.Write(this.ToStringHelper.ToStringWithCulture(_dbEntity.Item.DbSetName));
            this.Write("\r\n");
 foreach (var line in _find.Include()) { 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
 foreach (var line in _find.SingleOrDefault($"instanceKey.{InstanceKey.OBJECT_ARRAY}")) { 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
            this.Write("\r\n            if (entity == null) return null;\r\n\r\n            var aggregateInstan" +
                    "ce = ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_find.AggregateInstanceTypeFullName));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstance.FROM_DB_ENTITY_METHOD_NAME));
            this.Write("(entity);\r\n            return aggregateInstance;\r\n        }\r\n    }\r\n}\r\nnamespace " +
                    "");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregateInstance.Item.ClassName));
            this.Write(" {\r\n        /// <summary>\r\n        /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("のデータベースから取得した内容を画面に表示する形に変換します。\r\n        /// </summary>\r\n        public static ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregateInstance.Item.ClassName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstance.FROM_DB_ENTITY_METHOD_NAME));
            this.Write("(");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(_dbEntity.Item.ClassName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(E));
            this.Write(") {\r\n");
 PushIndent("            "); 
 FromDbEntity(); 
 PopIndent(); 
            this.Write("        }\r\n    }\r\n}\r\n#endregion 詳細検索\r\n\r\n\r\n#region 更新\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using Microsoft.AspNetCore.Mvc;\r\n    using ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(";\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_controller.ClassName));
            this.Write(" {\r\n        [HttpPost(\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(Controller.UPDATE_ACTION_NAME));
            this.Write("\")]\r\n        public virtual IActionResult Update(");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregateInstance.Item.ClassName));
            this.Write(" param) {\r\n            // TODO\r\n            throw new NotImplementedException();\r" +
                    "\n        }\r\n    }\r\n}\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n    using Microsoft.EntityFrameworkCore;\r\n  " +
                    "  using Microsoft.EntityFrameworkCore.Infrastructure;\r\n\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" {\r\n    }\r\n}\r\n#endregion 更新\r\n\r\n\r\n#region 削除\r\n#endregion 削除\r\n\r\n\r\n#region データ構造\r\nna" +
                    "mespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n    using Microsoft.EntityFrameworkCore;\r\n  " +
                    "  using Microsoft.EntityFrameworkCore.Infrastructure;\r\n    \r\n");
 foreach (var ett in _dbEntity.EnumerateThisAndDescendants()) { 
            this.Write("    /// <summary>\r\n    /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ett.GetCorrespondingAggregate().Item.DisplayName));
            this.Write("のデータベースに保存されるデータの形を表すクラスです。\r\n    /// </summary>\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ett.Item.ClassName));
            this.Write(" {\r\n");
 foreach (var col in ett.GetColumns()) { 
            this.Write("        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(col.CSharpTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(col.PropertyName));
            this.Write(" { get; set; }");
            this.Write(this.ToStringHelper.ToStringWithCulture(col.Initializer == null ? "" : $" = {col.Initializer};"));
            this.Write("\r\n");
 } 
            this.Write("\r\n");
 foreach (var nav in EnumerateNavigationProperties(ett)) { 
            this.Write("        public virtual ");
            this.Write(this.ToStringHelper.ToStringWithCulture(nav.CSharpTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(nav.PropertyName));
            this.Write(" { get; set; }");
            this.Write(this.ToStringHelper.ToStringWithCulture(nav.Initializer == null ? "" : $" = {nav.Initializer};"));
            this.Write("\r\n");
 } 
            this.Write("    }\r\n");
 } 
            this.Write("\r\n    partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" {\r\n");
 foreach (var ett in _dbEntity.EnumerateThisAndDescendants()) { 
            this.Write("        public DbSet<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(ett.Item.ClassName));
            this.Write("> ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ett.Item.DbSetName));
            this.Write(" { get; set; }\r\n");
 } 
            this.Write("    }\r\n}\r\n\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.RootNamespace));
            this.Write(" {\r\n    using System;\r\n    using System.Collections;\r\n    using System.Collection" +
                    "s.Generic;\r\n    using System.Linq;\r\n\r\n");
 foreach (var ins in _aggregateInstance.EnumerateThisAndDescendants()) { 
            this.Write("    /// <summary>\r\n    /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ins.GetCorrespondingAggregate().Item.DisplayName));
            this.Write("のデータ1件の詳細を表すクラスです。\r\n    /// </summary>\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ins.Item.ClassName));
            this.Write(" : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(AggregateInstance.BASE_CLASS_NAME));
            this.Write(" {\r\n");
 foreach (var prop in ins.GetProperties(_ctx.Config)) { 
            this.Write("        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.CSharpTypeName));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            this.Write(" { get; set; }\r\n");
 } 
            this.Write("    }\r\n");
 } 
            this.Write("\r\n    /// <summary>\r\n    /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("の一覧検索処理の検索条件を表すクラスです。\r\n    /// </summary>\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchCondition.ClassName));
            this.Write(" : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchCondition.BASE_CLASS_NAME));
            this.Write(" {\r\n");
 foreach (var prop in _searchCondition.GetMembers()) { 
            this.Write("        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Type.GetCSharpTypeName()));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Name));
            this.Write(" { get; set; }\r\n");
 } 
            this.Write("    }\r\n\r\n    /// <summary>\r\n    /// ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_aggregate.Item.DisplayName));
            this.Write("の一覧検索処理の検索結果1件を表すクラスです。\r\n    /// </summary>\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchResult.ClassName));
            this.Write(" : ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SearchResult.BASE_CLASS_NAME));
            this.Write(" {\r\n");
 foreach (var prop in _searchResult.GetMembers()) { 
            this.Write("        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Type.GetCSharpTypeName()));
            this.Write(" ");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.Name));
            this.Write(" { get; set; }\r\n");
 } 
            this.Write("    }\r\n}\r\n#endregion データ構造\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class AggFileBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        public System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
