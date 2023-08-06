﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HalApplicationBuilder.CodeRendering.EFCore {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using HalApplicationBuilder.Core;
    using System;
    
    
    public partial class DbContext : DbContextBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            this.Write("using Microsoft.EntityFrameworkCore;\r\n\r\nnamespace ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextNamespace));
            this.Write(" {\r\n\r\n    public partial class ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write(" : DbContext {\r\n        public ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write("(DbContextOptions<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.DbContextName));
            this.Write("> options) : base(options) { }\r\n\r\n");
 /* OnModelCreating定義 */ 
            this.Write("        /// <inheritdoc />\r\n        protected override void OnModelCreating(Model" +
                    "Builder modelBuilder) {\r\n");
 foreach (var dbEntity in _ctx.Schema.ToEFCoreGraph()) { 
            this.Write("\r\n            modelBuilder.Entity<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_ctx.Config.EntityNamespace));
            this.Write(".");
            this.Write(this.ToStringHelper.ToStringWithCulture(dbEntity.Item.ClassName));
            this.Write(">(");
            this.Write(this.ToStringHelper.ToStringWithCulture(ENTITY));
            this.Write(" => {\r\n");
 /* 主キー定義 */ 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ENTITY));
            this.Write(".HasKey(e => new {\r\n");
 foreach (var pk in dbEntity.GetColumns().Where(x => x.IsPrimary)) { 
            this.Write("                    e.");
            this.Write(this.ToStringHelper.ToStringWithCulture(pk.PropertyName));
            this.Write(",\r\n");
 } 
            this.Write("                });\r\n\r\n");
 /* 通常のプロパティの定義 */ 
 foreach (var col in dbEntity.GetColumns()) { 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(ENTITY));
            this.Write(".Property(e => e.");
            this.Write(this.ToStringHelper.ToStringWithCulture(col.PropertyName));
            this.Write(")\r\n                   .IsRequired(");
            this.Write(this.ToStringHelper.ToStringWithCulture(col.RequiredAtDB ? "true" : "false"));
            this.Write(");\r\n");
 } 
            this.Write("\r\n");
 /* ナビゲーションプロパティ定義 */ 
 foreach (var line in RenderNavigationPropertyOnModelCreating(dbEntity)) { 
            this.Write("                ");
            this.Write(this.ToStringHelper.ToStringWithCulture(line));
            this.Write("\r\n");
 } 
            this.Write("            });\r\n");
 } 
            this.Write("        }\r\n    }\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class DbContextBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((value != null)) {
                        this.formatProvider = value;
                    }
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
