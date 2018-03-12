F# Docset for Dash
==================

An F# DocSet for the [Dash][d] documentation browser.

Please keep in mind that this DocSet is using the [**legacy** visualfsharpdocs repo][vsd], which is
in the process of being moved to the [dotnet docs repo][dd]. Unfortunatly the core library is still
missing from the new repo, so the legacy repo is all we have right now apparently.

TODO
----

- fix HTML Entities like `append: seq&lt;'T&gt; -&gt; seq&lt;'T&gt; -&gt; seq&lt;'T&gt;`
- Rewrite msdn links to local links where possible
- Write the style.css
- Find out about the other EntryTypes
- Remove meta data header before generating HTML
- TOC
- Turn it async already

[d]: https://kapeli.com/dash
[vsd]: https://github.com/MicrosoftDocs/visualfsharpdocs
[dd]: https://github.com/dotnet/docs
