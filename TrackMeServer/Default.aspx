<%@ Page Language="C#" Inherits="TrackMeServer.Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<head runat="server">
	<title>Default</title>
</head>
<body>
	<form id="form1" runat="server">
		<asp:Button id="button1" runat="server" Text="Click me!" OnClick="button1Clicked" />
	</form>
<ul>
<%

foreach (var s in TrackMeServer.TrackService.GetAllShares ()) {
%>
<li><a href="<%="https://app.trackme/" + s.PublicId%>"><%=s.PublicId%></a></li>
<% } %>
</ul>
</body>
</html>
