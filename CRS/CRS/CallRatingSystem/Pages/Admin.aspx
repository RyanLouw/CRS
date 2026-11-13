<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/Main.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="CallRatingSystem.Pages.Admin" Async="true" %>

<%@ Register Src="~/Controls/Admin/AdminControl.ascx" TagPrefix="uc1" TagName="AdminControl" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <uc1:AdminControl runat="server" ID="AdminControl" />
</asp:Content>
