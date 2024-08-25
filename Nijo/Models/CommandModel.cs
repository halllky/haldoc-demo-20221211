using Nijo.Core;
using Nijo.Models.CommandModelFeatures;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// コマンド。
    /// ユーザーがパラメータを指定し、サーバー側で同期的に実行される処理。
    /// スキーマ定義で設定されたデータ構造はコマンドのパラメータを表す。
    /// </summary>
    internal class CommandModel : IModel {
        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var commandFile = context.UseSummarizedFile<CoreLibraryFile>();

            // データ型: パラメータ型定義
            var parameter = new CommandParameter(rootAggregate);
            commandFile.AddClassDeclaring(parameter.RenderCSharpDeclaring(context));
            context.ReactProject.Types.Add(parameter.RenderTsDeclaring(context));

            // データ型: エラーメッセージ
            commandFile.AddClassDeclaring(parameter.RenderCSharpErrorClassDeclaring(context));

            // 処理: Reactフック、Webエンドポイント、本処理抽象メソッド
            var commandMethod = new CommandMethod(rootAggregate);
            context.ReactProject.AutoGeneratedHook.Add(commandMethod.RenderHook(context));
            context.UseSummarizedFile<CommandController>().AddAction(commandMethod.RenderController(context));
            commandFile.AddApplicationServiceMethod(commandMethod.RenderAbstractMethod(context));

            // UI: ダイアログ
            var dialog = new CommandDialog(rootAggregate);
            context.ReactProject.AutoGeneratedComponents.Add(dialog.RenderHook(context));

            // 処理: クライアント側新規オブジェクト作成関数
            context.ReactProject.Types.Add(parameter.RenderTsNewObjectFunction(context));
        }

        void IModel.GenerateCode(CodeRenderingContext context) {
            // データ型: 処理結果型（TypeScript）
            context.ReactProject.Types.Add(CommandResult.RenderTsDeclaring(context));

            // サーバー側処理結果ハンドラ
            context.CoreLibrary.UtilDir(dir => {
                dir.Generate(CommandResult.RenderInterface(context));
            });
            context.WebApiProject.UtilDir(dir => {
                dir.Generate(CommandResult.RenderResultHandlerInWeb(context));
            });
            context.CliProject.AutoGeneratedDir(dir => {
                dir.Generate(CommandResult.RenderResultHandlerInCommandLine(context));
            });

            // 処理: 共通呼び出しフック
            context.ReactProject.AutoGeneratedHook.Add(CommandMethod.RenderCommonHook(context));
        }

        IEnumerable<string> IModel.ValidateAggregate(GraphNode<Aggregate> rootAggregate) {
            foreach (var aggregate in rootAggregate.EnumerateThisAndDescendants()) {
                foreach (var member in aggregate.GetMembers()) {

                    // キー指定不可
                    if (member is AggregateMember.ValueMember vm && vm.IsKey
                        || member is AggregateMember.Ref @ref && @ref.Relation.IsPrimary()) {
                        yield return $"{aggregate.Item.DisplayName}.{member.MemberName}: {nameof(CommandModel)}のメンバーをキーに指定することはできません。";
                    }
                }
            }
        }
    }
}
