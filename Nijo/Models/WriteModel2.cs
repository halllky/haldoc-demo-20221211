using Nijo.Core;
using Nijo.Models.RefTo;
using Nijo.Models.WriteModel2Features;
using Nijo.Parts;
using Nijo.Parts.WebClient;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models {
    /// <summary>
    /// 登録・更新・削除される単位のデータ
    /// </summary>
    internal class WriteModel2 : IModel {
        void IModel.GenerateCode(CodeRenderingContext context, GraphNode<Aggregate> rootAggregate) {
            var allAggregates = rootAggregate.EnumerateThisAndDescendants();
            var aggregateFile = context.CoreLibrary.UseAggregateFile(rootAggregate);
            var uiContext = context.UseSummarizedFile<UiContext>();

            // データ型: 登録更新コマンドベース
            context.UseSummarizedFile<DataClassForSaveBase>().Register(rootAggregate);

            foreach (var agg in allAggregates) {
                // データ型: EFCore Entity
                var efCoreEntity = new EFCoreEntity(agg);
                aggregateFile.DataClassDeclaring.Add(efCoreEntity.Render(context));

                var dbContext = context.UseSummarizedFile<Parts.WebServer.DbContextClass>();
                dbContext.AddDbSet(efCoreEntity.ClassName, efCoreEntity.DbSetName);
                dbContext.AddOnModelCreating(efCoreEntity.RenderCallingOnModelCreating(context));

                // データ型: DataClassForNewItem
                var dataClassForNewItem = new DataClassForSave(agg, DataClassForSave.E_Type.Create);
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForNewItem.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForNewItem.RenderTypeScriptReadOnlyStructure(context));

                // データ型: DataClassForSave
                var dataClassForSave = new DataClassForSave(agg, DataClassForSave.E_Type.UpdateOrDelete);
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharp(context));
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpMessageStructure(context)); // メッセージクラスはCreate/Saveで共用
                aggregateFile.DataClassDeclaring.Add(dataClassForSave.RenderCSharpReadOnlyStructure(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScript(context));
                context.ReactProject.Types.Add(rootAggregate, dataClassForSave.RenderTypeScriptReadOnlyStructure(context));

                // 処理: DataClassForSave, DataClassForNewItem 新規作成関数
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    context.ReactProject.Types.Add(dataClassForNewItem.RenderTsNewObjectFunction(context));
                    context.ReactProject.Types.Add(dataClassForSave.RenderTsNewObjectFunction(context));
                }
            }

            // データ型: 一括更新処理 エラーメッセージの入れ物
            context.UseSummarizedFile<SaveContext>().AddWriteModel(rootAggregate);

            // 処理: 一括更新処理
            context.UseSummarizedFile<BatchUpdateWriteModel>().Register(rootAggregate);

            // 処理: 新規作成処理 AppSrv
            // 処理: 更新処理 AppSrv
            // 処理: 削除処理 AppSrv
            var create = new CreateMethod(rootAggregate);
            var update = new UpdateMethod(rootAggregate);
            var delete = new DeleteMethod(rootAggregate);
            aggregateFile.AppServiceMethods.Add(create.Render(context));
            aggregateFile.AppServiceMethods.Add(update.Render(context));
            aggregateFile.AppServiceMethods.Add(delete.Render(context));

            // 処理: 必須チェックメソッド
            aggregateFile.AppServiceMethods.Add(RequiredCheck.Render(rootAggregate, context));

            // 処理: SetReadOnly AppSrv
            var setReadOnly = new SetReadOnly(rootAggregate);
            aggregateFile.AppServiceMethods.Add(setReadOnly.Render(context));


            if (rootAggregate.Item.Options.GenerateDefaultReadModel) {
                // 既定のReadModel（WriteModelと同じ型のReadModel）を生成する
               context.GetModel<ReadModel2>().GenerateCode(context, rootAggregate);

            } else {
                // 既定のReadModelが無い場合でも他の集約から参照されるときのための部品は必要になるので生成する
                foreach (var agg in allAggregates) {

                    // パフォーマンス改善のため、ほかの集約から参照されていない集約のRefTo部品は生成しない
                    if (!context.Config.GenerateUnusedRefToModules && !agg.GetReferedEdges().Any()) {
                        continue;
                    }

                    var asEntry = agg.AsEntry();

                    // データ型
                    var refTargetKeys = new DataClassForRefTargetKeys(asEntry, asEntry);
                    var refSearchCondition = new RefSearchCondition(asEntry, asEntry);
                    var refSearchResult = new RefSearchResult(asEntry, asEntry);
                    var refDisplayData = new RefDisplayData(asEntry, asEntry);
                    aggregateFile.DataClassDeclaring.Add(refTargetKeys.RenderCSharpDeclaringRecursively(context));
                    aggregateFile.DataClassDeclaring.Add(refSearchCondition.RenderCSharpDeclaringRecursively(context));
                    aggregateFile.DataClassDeclaring.Add(refSearchResult.RenderCSharp(context));
                    aggregateFile.DataClassDeclaring.Add(refDisplayData.RenderCSharp(context));
                    context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderTypeScriptDeclaringRecursively(context));
                    context.ReactProject.Types.Add(rootAggregate, refSearchCondition.RenderCreateNewObjectFn(context));
                    context.ReactProject.Types.Add(rootAggregate, refTargetKeys.RenderTypeScriptDeclaringRecursively(context));
                    context.ReactProject.Types.Add(rootAggregate, refDisplayData.RenderTypeScript(context));
                    context.ReactProject.Types.Add(rootAggregate, refDisplayData.RenderTsNewObjectFunction(context));

                    // UI: 詳細画面用のVFormの一部
                    // UI: 検索条件欄のVFormの一部
                    // UI: コンボボックス
                    // UI: 検索ダイアログ
                    // UI: インライン検索ビュー
                    var refToFile = context.UseSummarizedFile<RefToFile>();
                    var comboBox = new SearchComboBox(asEntry);
                    var searchDialog = new SearchDialog(asEntry, asEntry);
                    var inlineRef = new SearchInline(asEntry);
                    refToFile.Add(asEntry, refDisplayData.RenderSingleViewUiComponent(context));
                    refToFile.Add(asEntry, refSearchCondition.RenderUiComponent(context));
                    refToFile.Add(asEntry, comboBox.Render(context));
                    refToFile.Add(asEntry, searchDialog.RenderHook(context));
                    refToFile.Add(asEntry, inlineRef.Render(context));
                    searchDialog.RegisterUiContext(uiContext);
                    refDisplayData.RegisterUiContext(uiContext);
                    refSearchCondition.RegisterUiContext(uiContext);

                    // UI: DataTable用の列
                    var refToColumn = new DataTableRefColumnHelper(asEntry);
                    context.UseSummarizedFile<Parts.WebClient.DataTable.CellType>().Add(refToColumn.Render(context));

                    // 処理: 参照先検索
                    var searchRef = new RefSearchMethod(asEntry, asEntry);
                    refToFile.Add(asEntry, searchRef.RenderHook(context));
                    aggregateFile.ControllerActions.Add(searchRef.RenderController(context));
                    aggregateFile.AppServiceMethods.Add(searchRef.RenderAppSrvMethodOfWriteModel(context));
                }
            }

            // ---------------------------------------------
            // 処理: デバッグ用ダミーデータ作成関数
            context.UseSummarizedFile<DummyDataGenerator>().Add(rootAggregate);
        }

        void IModel.GenerateCode(CodeRenderingContext context) {
        }

        IEnumerable<string> IModel.ValidateAggregate(GraphNode<Aggregate> rootAggregate) {
            foreach (var agg in rootAggregate.EnumerateThisAndDescendants()) {

                // ルート集約またはChildrenはキー必須
                if (agg.IsRoot() || agg.IsChildrenMember()) {
                    var ownKeys = agg
                        .GetKeys()
                        .Where(m => m is AggregateMember.ValueMember vm && vm.DeclaringAggregate == vm.Owner
                                 || m is AggregateMember.Ref);
                    if (!ownKeys.Any()) {
                        yield return $"{agg.Item.DisplayName}にキーが1つもありません。";
                    }
                }

                // HasLifecycleはReadModel専用の属性
                if (agg.Item.Options.HasLifeCycle) {
                    yield return
                        $"{agg.Item.DisplayName}: {nameof(AggregateBuildOption.HasLifeCycle)}は{nameof(ReadModel2)}専用の属性です。" +
                        $"{nameof(WriteModel2)}でライフサイクルが分かれる部分は別のルート集約として定義し、元集約への参照をキーにすることで表現してください。";
                }

                foreach (var member in agg.GetMembers()) {

                    // WriteModelからReadModelへの参照は不可
                    if (member is AggregateMember.Ref @ref
                        && @ref.RefTo.GetRoot().Item.Options.Handler != NijoCodeGenerator.Models.WriteModel2.Key) {

                        yield return $"{agg.Item.DisplayName}.{member.MemberName}: {nameof(WriteModel2)}の参照先は{nameof(WriteModel2)}である必要があります。";
                    }
                }
            }
        }
    }
}
