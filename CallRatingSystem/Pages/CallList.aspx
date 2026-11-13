<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="CallList.aspx.cs" Inherits="CallRatingSystem.Pages.CallList" Async="true" %>

<%@ Register Src="~/Controls/Operator/CallListControl.ascx" TagPrefix="uc1" TagName="CallListControl" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <uc1:CallListControl runat="server" id="CallListControl" />
</asp:Content>

