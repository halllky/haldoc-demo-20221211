// ------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン: 17.0.0.0
//  
//     このファイルへの変更は、正しくない動作の原因になる可能性があり、
//     コードが再生成されると失われます。
// </auto-generated>
// ------------------------------------------------------------------------------
namespace HalApplicationBuilder.CodeRendering.ReactAndWebApi
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class ReactComponentTemplate : ReactComponentTemplateBase
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"import React, { useState, useCallback } from 'react';
import { useCtrlS } from './hooks/useCtrlS';
import { useAppContext } from './hooks/AppContext';
import { AgGridReact } from 'ag-grid-react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';
import { FieldValues, SubmitHandler, useForm } from 'react-hook-form';
import { BookmarkIcon, ChevronDownIcon, ChevronUpIcon, MagnifyingGlassIcon, PlusIcon, BookmarkSquareIcon } from '@heroicons/react/24/outline';
import { IconButton } from './components/IconButton';
import { ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchCondition.ClassName));
            this.Write(", ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchResult.ClassName));
            this.Write("/*, ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_uiInstance.ClassName));
            this.Write(" */ } from \'");
            this.Write(this.ToStringHelper.ToStringWithCulture(GetImportFromTypes()));
            this.Write("\';\r\n\r\nexport const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(MultiViewComponentName));
            this.Write(" = () => {\r\n\r\n    const [{ apiDomain }, dispatch] = useAppContext()\r\n    useCtrlS" +
                    "(() => {\r\n        dispatch({ type: \'pushMsg\', msg: \'保存しました。\' })\r\n    })\r\n\r\n    c" +
                    "onst [param, setParam] = useState<");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchCondition.ClassName));
            this.Write(">({} as ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchCondition.ClassName));
            this.Write(") // TODO\r\n    const { register, handleSubmit, reset } = useForm()\r\n    const onS" +
                    "earch: SubmitHandler<FieldValues> = useCallback(data => {\r\n        setParam(data" +
                    " as ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchCondition.ClassName));
            this.Write(")\r\n    }, [])\r\n    const onClear = useCallback((e: React.MouseEvent) => {\r\n      " +
                    "  reset()\r\n        e.preventDefault()\r\n    }, [reset])\r\n    const { data, isLoad" +
                    "ing, error } = useQuery({\r\n        queryKey: [\'");
            this.Write(this.ToStringHelper.ToStringWithCulture(_rootAggregate.GetGuid()));
            this.Write("\', JSON.stringify(param)],\r\n        queryFn: async () => {\r\n            const jso" +
                    "n = JSON.stringify(param)\r\n            const encoded = window.encodeURI(json)\r\n " +
                    "           const response = await fetch(`${apiDomain}/");
            this.Write(this.ToStringHelper.ToStringWithCulture(_rootAggregate.GetCSharpSafeName()));
            this.Write("/list?param=${encoded}`)\r\n            if (!response.ok) throw new Error(\'Network " +
                    "response was not OK.\')\r\n            return (await response.json()) as ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_searchResult.ClassName));
            this.Write("[]\r\n        },\r\n    })\r\n\r\n    const navigate = useNavigate()\r\n    const toCreateV" +
                    "iew = useCallback(() => {\r\n        navigate(\'");
            this.Write(this.ToStringHelper.ToStringWithCulture(CreateViewUrl));
            this.Write(@"')
    }, [navigate])

    const [expanded, setExpanded] = useState(true)
    
    if (error) return <p>Error: {JSON.stringify(error)}</p>

    return (
        <div className=""ag-theme-alpine compact h-full w-full"">

            <div className=""flex flex-row justify-start items-center space-x-1"">
                <div className='flex flex-row items-center space-x-1 cursor-pointer' onClick={() => setExpanded(!expanded)}>
                    <h1 className=""text-base font-semibold select-none py-1"">
                        ");
            this.Write(this.ToStringHelper.ToStringWithCulture(_rootAggregate.GetDisplayName()));
            this.Write(@"
                    </h1>
                    {expanded
                        ? <ChevronDownIcon className=""w-4"" />
                        : <ChevronUpIcon className=""w-4"" />}
                </div>
                <div className='flex-1'></div>
                <IconButton icon={PlusIcon} onClick={toCreateView}>新規作成</IconButton>
            </div>

            {expanded &&
                <form className='flex flex-col space-y-1 py-1' onSubmit={handleSubmit(onSearch)}>
");
 PushIndent("                    "); 
 _rootAggregate.RenderReactSearchCondition(new RenderingContext(this, new ObjectPath("searchCondition"))); 
 PopIndent(); 
            this.Write(@"                    <div className='flex flex-row justify-start space-x-1'>
                        <IconButton icon={MagnifyingGlassIcon}>検索</IconButton>
                        <IconButton outline onClick={onClear}>クリア</IconButton>
                        <div className=""flex-1""></div>
                        <IconButton outline icon={BookmarkIcon}>この検索条件を保存</IconButton>
                    </div>
                </form>
            }

            <AgGridReact
                rowData={isLoading ? [] : data}
                columnDefs={columnDefs}
                multiSortKey='ctrl'
                undoRedoCellEditing
                undoRedoCellEditingLimit={20}>
            </AgGridReact>
        </div>
    )
}

const columnDefs = [
    {
        resizable: true,
        cellRenderer: ({ data }: any) => {
            // console.log(data)
            return <Link to=""/"" className=""text-blue-400"">詳細</Link>
        },
    },
");
 foreach (var prop in _searchResult.Properties) { 
            this.Write("    { field: \'");
            this.Write(this.ToStringHelper.ToStringWithCulture(prop.PropertyName));
            this.Write("\', resizable: true, sortable: true, editable: true },\r\n");
 } 
            this.Write("]\r\n\r\nexport const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(CreateViewComponentName));
            this.Write(" = () => {\r\n\r\n    const { register } = useForm()\r\n\r\n    return (\r\n        <div cl" +
                    "assName=\"flex flex-col justify-start space-y-1\">\r\n            <h1 className=\"tex" +
                    "t-base font-semibold select-none py-1\">\r\n                <Link to=\"");
            this.Write(this.ToStringHelper.ToStringWithCulture(MultiViewUrl));
            this.Write("\">");
            this.Write(this.ToStringHelper.ToStringWithCulture(_rootAggregate.GetDisplayName()));
            this.Write("</Link> &#047; 新規作成\r\n            </h1>\r\n            <div className=\"flex-1 flex f" +
                    "lex-col space-y-1\">\r\n");
 PushIndent("                "); 
 _rootAggregate.RenderReactSearchCondition(new RenderingContext(this, new ObjectPath("instance"))); 
 PopIndent(); 
            this.Write("            </div>\r\n            <IconButton icon={BookmarkSquareIcon} className=\"" +
                    "self-start\">保存</IconButton>\r\n        </div>\r\n    )\r\n}\r\n\r\nexport const ");
            this.Write(this.ToStringHelper.ToStringWithCulture(SingleViewComponentName));
            this.Write(" = () => {\r\n\r\n    const { register } = useForm()\r\n\r\n    return (\r\n        <div cl" +
                    "assName=\"flex flex-col justify-start space-y-1\">\r\n");
 PushIndent("            "); 
 _rootAggregate.RenderReactSearchCondition(new RenderingContext(this, new ObjectPath("instance"))); 
 PopIndent(); 
            this.Write("        </div>\r\n    )\r\n}\r\n");
            return this.GenerationEnvironment.ToString();
        }
    }
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class ReactComponentTemplateBase
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
        protected System.Text.StringBuilder GenerationEnvironment
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
