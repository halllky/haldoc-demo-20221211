
namespace haldoc.AutoGenerated.Mvc {
    using System.Collections.Generic;

    public class 取引先__SearchCondition {
        public string 企業ID { get; set; }
        public string 企業名 { get; set; }
        public haldoc.E_重要度? 重要度 { get; set; }
        public bool 資本情報__上場企業資本情報 { get; set; }
        public bool 資本情報__非上場企業資本情報 { get; set; }
    }
    public class 請求情報__SearchCondition {
        public string 取引先 { get; set; }
        public string 宛名 { get; set; }
        public string 郵便番号 { get; set; }
        public string 都道府県 { get; set; }
        public string 市町村 { get; set; }
        public string 丁番地 { get; set; }
    }
    public class 取引先支店__SearchCondition {
        public string 会社 { get; set; }
        public haldoc.Core.FromTo<int?> 支店連番 { get; set; }
        public string 支店名 { get; set; }
    }
    public class 営業所__SearchCondition {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
    }
    public class 担当者__SearchCondition {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
        public string 所属 { get; set; }
    }

    public class 取引先__ListItem {
        public string 企業ID { get; set; }
        public string 企業名 { get; set; }
        public haldoc.E_重要度? 重要度 { get; set; }
        public string 資本情報 { get; set; }
    }
    public class 請求情報__ListItem {
        public string 取引先 { get; set; }
        public string 取引先__ID { get; set; }
        public string 宛名 { get; set; }
        public string 郵便番号 { get; set; }
        public string 都道府県 { get; set; }
        public string 市町村 { get; set; }
        public string 丁番地 { get; set; }
    }
    public class 取引先支店__ListItem {
        public string 会社 { get; set; }
        public string 会社__ID { get; set; }
        public int 支店連番 { get; set; }
        public string 支店名 { get; set; }
    }
    public class 営業所__ListItem {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
    }
    public class 担当者__ListItem {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
        public string 所属 { get; set; }
        public string 所属__ID { get; set; }
    }

    public class 取引先 {
        public string 企業ID { get; set; }
        public string 企業名 { get; set; }
        public haldoc.E_重要度? 重要度 { get; set; }
        public object 資本情報 { get; set; }
        public List<haldoc.AutoGenerated.Mvc.コメント> 備考 { get; set; } = new();
        public List<haldoc.AutoGenerated.Mvc.担当者> 担当者 { get; set; } = new();
    }
    public class 請求情報 {
        public haldoc.Runtime.ExternalObject 取引先 { get; set; }
        public string 宛名 { get; set; }
        public haldoc.AutoGenerated.Mvc.連絡先 住所 { get; set; } = new();
    }
    public class 取引先支店 {
        public haldoc.Runtime.ExternalObject 会社 { get; set; }
        public int 支店連番 { get; set; }
        public string 支店名 { get; set; }
    }
    public class 営業所 {
        public string 営業所ID { get; set; }
        public string 営業所名 { get; set; }
    }
    public class 担当者 {
        public string ユーザーID { get; set; }
        public string 氏名 { get; set; }
        public haldoc.Runtime.ExternalObject 所属 { get; set; }
    }
    public class 上場企業資本情報 {
        public decimal 自己資本比率 { get; set; }
        public decimal 利益率 { get; set; }
    }
    public class 非上場企業資本情報 {
        public string 主要株主 { get; set; }
        public haldoc.E_安定性 安定性 { get; set; }
    }
    public class コメント {
        public string Text { get; set; }
        public string At { get; set; }
        public haldoc.Runtime.ExternalObject By { get; set; }
    }
    public class 連絡先 {
        public string 郵便番号 { get; set; }
        public string 都道府県 { get; set; }
        public string 市町村 { get; set; }
        public string 丁番地 { get; set; }
    }

}