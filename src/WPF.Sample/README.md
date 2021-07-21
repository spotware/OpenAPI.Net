# WPF.Sample

This demo is a WPF application, it uses CefSharp to show the symbols chart and for the account authorizatrion.

You must install Visual C++ 2019 x86 on your system before using this application, and you have to run it on either x86/x64 configurations, not on Any CPU, otherwise CefSharp will not work.

Your API application redirerct URI must be a valid working URL, otherwise the cefsharp will not redirect to it and the app will not be able to get the auth code.
