<!doctype html>
<html xmlns="http://www.w3.org/1999/xhtml">
	<head>
		<title>NBA Statistics</title>
		<link rel="stylesheet" type="text/css" href="54.186.244.225/PA1/index.css" />
		<script src="http://ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js" type="text/javascript"></script>
		<script type="text/javascript">
			$(document).ready(function(){
				$("#Search").click(function(){
					$.ajax({
						type: "POST",
						data: {input: $("#input").val()},
						url: "search.php",
						dataType: "json",
						error: function() { alert('An error has occured. Submit again.'); },
						success: jParse
					});
				});
				function jParse(json){
					$(".display").html('');
					for(var i = 0; i < json.length; i++){
						$(".display").append("<div id='player_name'>" + json[i].PlayerName + "</div>");
						$(".display").append("<table id='player_stats'><tr id='stats_name'><td>GP</td><td>FGP</td><td>TPP</td><td>FTP</td><td>PPG</td></tr><tr id='stats_num'><td>" + json[i].GP + "</td><td>" + json[i].FGP + "</td><td>" + json[i].TPP + "</td><td>" + json[i].FTP + "</td><td>" + json[i].PPG + "</td></tr>");
					}
					$(".display").append("</table>");
				}
			});
		</script>
	</head>
	<body>
		<div id="logo"><img src="http://www2.lvc.edu/lavie/files/2013/11/NBA-Logo-Vector-PNG.png" width="626px"></div>
		<h1>NBA Statistics</h1>
		<div class="menu">
			<a href="<?php echo $_SERVER['PHP_SELF']; ?>">Home</a>
		</div>

		<div class="main">
			<form onsubmit="return false;">
				<input type="text" id="input" size="67">
				<input type="submit" value="Search" name="op" id="Search">
			</form>
		</div>

		<div class="display">
		</div>

	</body>
</html>
