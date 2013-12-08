<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SessionStart.aspx.cs" Inherits="RedisSessionWebSample.SessionStart" EnableViewState="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Redis Session Manipulations</title>
    <style type="text/css">
        body 
        {
            margin: 2em;
            font-size: 0.8em;
            font-family: Verdana, Arial;
        }
        p, ul, li {
               padding-top: 2px;
               padding-bottom: 4px;
               margin: 0px;
        }
        h5 {
            color: #800;
            font-size: 1.1em;
            margin: 0px;
            padding-top: 2px;
            padding-bottom: 2px;
        }
        .inner_div {
            background-color: #e8ffe8;
            margin: 4px;
            padding: 1em;
            font-size: 0.95em;
        }
        .outer_div {
            background-color: #EFEFEF;
            margin: 4px;
            padding: 1em;
            font-size: 0.95em;
            border: 1px solid #ccc;
        }
        .btn_bar {
            display: block;
            background-color: #ddd;
            margin: 0px;
            padding: 1em;
            font-size: 0.85em;
            border-top: 1px solid #eee;
            border-bottom: 1px solid #888;
        }
    </style>
</head>
<body>
     <h1>RediSession Sample Test Page</h1>
     <h4>Click a button to get started</h4>
    <form id="form1" runat="server">
    
    <div class="btn_bar">
        <asp:Button ID="ButCreateNewValues" runat="server" Text="Create Session Values" OnClick="ButCreateNewValues_Click" /> 
        <asp:Button ID="ButCompare" runat="server" Text="Compare Session Values" OnClick="ButCompare_Click" /> 
        <asp:Button ID="ButClear" runat="server" Text="Clear Values" OnClick="ButClear_Click" /> 
        <asp:Button ID="ButNuke" runat="server" Text="Clear Session" OnClick="ButNuke_Click" /> 
    </div>
    <div class="outer_div">
        <h4>Current Session Values:</h4>
        <asp:Literal ID="litOutput" runat="server" Mode="PassThrough" />
    </div>
    </form>
</body>
</html>
