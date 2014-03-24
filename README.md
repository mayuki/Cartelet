Cartelet
========
[![Build status](https://ci.appveyor.com/api/projects/status/ngcm4pv7wxhors7l)](https://ci.appveyor.com/project/mayuki/cartelet)

Cartelet(カータレット)はHTMLをパースしてフィルターするためのライブラリです。

CSSセレクターにマッチした要素の属性を書き換えたり出力にフィルターを施すことができます。

アリス「わたし、何もあげられるもの無いからHTMLをフィルターするよ！」


できること
-------
- HTMLの**それなりに高速**でそれなりなパース
- 出力時にCSSセレクターで要素に対してマッチして属性や出力フィルター処理
- フィルターしない部分は極力非破壊
- ASP.NET MVCのViewEngine対応
- CSSのstyle属性への展開 (Cartelet.StylesheetExpander)


できないこと
--------
- 高度で厳密なHTMLのパース(閉じタグの省略等)
- DOMのような操作(要素の追加、移動等)
- TextWriterを通すような一律変換等を除いた要素の内容書き換え

厳密なHTMLの解釈を必要とする(例えば閉じタグの省略など)場合にはSgmlReaderなどのパーサーを、CSSセレクターとDOM操作のようなことや内容の変更(つまりjQueryのようなこと)をしたい場合にはCsQueryをお勧めします。

あくまでそれなりにHTMLをパースしてそれなりに扱え、それなりに速いのが特徴です。

機能が限定されている反面、複雑度にもよりますがHtmlAgilityPackの2倍、SgmlReaderの3~4倍、CsQueryの5倍ぐらいの速度で処理できます。

例えば…
-------------
- HTMLを読み込んで特定のclassが指定されている部分のみ、大文字英字変換をかける
- HTMLを読み込んで特定の属性が指定されている要素の属性を書き換える
- などなど


インストール
--------
NuGetで「Cartelet」を検索してインストール、または以下のコマンドをパッケージマネージャーコンソールで実行してください。

```
Install-Package Cartelet
```

- https://www.nuget.org/packages/Cartelet/
- https://www.nuget.org/packages/Cartelet.Mvc/
- https://www.nuget.org/packages/Cartelet.StylesheetExpander/

サンプルコード
---------
```cs
var content = @"
<!DOCTYPE html>
<title>hauhau</title>
<ul>
<li><a href=""#"">1</a></li>
<li><a href=""#"">2</a></li>
<li><a href=""#"">3</a></li>
<li><a href=""#"">4</a></li>
</ul>
";
    
var htmlFilter = new HtmlFilter();
var node = HtmlParser.Parse(content);
var sw = new StringWriter();
var context = new CarteletContext(content, sw);
    
htmlFilter.AddHandler("li:nth-child(2n)",
                      (ctx, nodeInfo) => { nodeInfo.Attributes["style"] = "color:red;"; return true; });
    
htmlFilter.Execute(context, node);
    
sw.ToString().Is(@"
<!DOCTYPE html>
<title>hauhau</title>
<ul>
<li><a href=""#"">1</a></li>
<li style=""color:red;""><a href=""#"">2</a></li>
<li><a href=""#"">3</a></li>
<li style=""color:red;""><a href=""#"">4</a></li>
</ul>
");
```

CSSセレクターの制限
------------------
Selectors Level 3 ( http://www.w3.org/TR/css3-selectors/ )相当のセレクターに対応しています。ただし下記の一部セレクターには非対応です。

- :not
- 疑似要素 (::before, ::after, ::first-line, ::first-letter)
    - StylesheetExpanderは::before/::afterを限定的に利用可能
- UI要素の疑似クラス (:enabled, :checked, :indeterminate)
- :empty 疑似クラス
- :root 疑似クラス
- :nth-last-child, :nth-last-of-type 疑似クラス
- :link, :visited, :hover, :active, :focus 動的疑似クラス

必要な環境
---------
Microsoft .NET Framework 4.5以上

ライセンス
-------
MIT License

Copyright © Mayuki Sawatari <mayuki@misuzilla.org>
