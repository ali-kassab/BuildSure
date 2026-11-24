<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BuildSure.WebForms._Default" %>
<%--
    ASPX page = markup + server controls (<asp:...> with runat="server").
    The whole page is ONE <form runat="server">. Every button click POSTS this form back to the
    same page (a "PostBack"), and the hidden __VIEWSTATE field carries control state between requests.
--%>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>BuildSure - Web Forms demo</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 2rem; background: #f5f6f8; color: #212529; }
        .box { background: #fff; border: 1px solid #dee2e6; border-radius: 6px; padding: 1rem 1.25rem; margin-bottom: 1rem; max-width: 900px; }
        h1 { font-size: 1.5rem; } h2 { font-size: 1.1rem; margin-top: 0; }
        .muted { color: #6c757d; font-size: .9rem; }
        .grid { border-collapse: collapse; width: 100%; }
        .grid th, .grid td { border: 1px solid #dee2e6; padding: .35rem .5rem; text-align: left; }
        .grid th { background: #e9ecef; }
        .error { color: #b02a37; }
        input[type=submit] { padding: .3rem .8rem; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h1>BuildSure &middot; legacy Web Forms demo</h1>
        <p class="muted">Training simulation. This page is a separate .NET Framework 4.8 project - it is not part of the ASP.NET Core MVC app.</p>

        <!-- 1. PostBack -->
        <div class="box">
            <h2>1. PostBack</h2>
            <asp:Label ID="lblPostBack" runat="server" />
        </div>

        <!-- 2. Server controls + Button click event + database -->
        <div class="box">
            <h2>2. Search applications (server controls + Click event)</h2>
            Search:
            <asp:TextBox ID="txtSearch" runat="server" placeholder="Number, project or customer" Width="260px" />
            Status:
            <asp:DropDownList ID="ddlStatus" runat="server">
                <asp:ListItem Text="All" Value="" />
                <asp:ListItem Text="Pending" Value="Pending" />
                <asp:ListItem Text="Approved" Value="Approved" />
                <asp:ListItem Text="Rejected" Value="Rejected" />
            </asp:DropDownList>
            <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="btnSearch_Click" />
            <p><asp:Label ID="lblSearchResult" runat="server" CssClass="muted" /></p>
            <asp:GridView ID="gvApplications" runat="server" CssClass="grid" AutoGenerateColumns="false"
                          EmptyDataText="No applications found." GridLines="None">
                <Columns>
                    <asp:BoundField DataField="ApplicationNumber" HeaderText="Number" />
                    <asp:BoundField DataField="CustomerName" HeaderText="Customer" />
                    <asp:BoundField DataField="ProjectName" HeaderText="Project" />
                    <asp:BoundField DataField="Status" HeaderText="Status" />
                    <asp:BoundField DataField="CertificateNumber" HeaderText="Certificate" />
                </Columns>
            </asp:GridView>
        </div>

        <!-- 3. ViewState -->
        <div class="box">
            <h2>3. ViewState</h2>
            <p>
                <asp:Button ID="btnIncrement" runat="server" Text="Click me" OnClick="btnIncrement_Click" />
                &nbsp;Clicks stored in <code>ViewState["Clicks"]</code>:
                <strong><asp:Label ID="lblClicks" runat="server" Text="0" /></strong>
            </p>
            <p>
                Label WITH ViewState: <strong><asp:Label ID="lblWithViewState" runat="server" Text="(not set)" /></strong><br />
                Label WITHOUT ViewState: <strong><asp:Label ID="lblWithoutViewState" runat="server" Text="(not set)" EnableViewState="false" /></strong>
            </p>
            <asp:Button ID="btnSetLabels" runat="server" Text="Set both labels" OnClick="btnSetLabels_Click" />
            <asp:Button ID="btnPlainPostBack" runat="server" Text="Do a plain PostBack" />
            <p class="muted">Click "Set both labels", then "Do a plain PostBack": the label without ViewState resets to its markup value,
               because nothing remembered the change between requests.</p>
        </div>
    </form>
</body>
</html>
