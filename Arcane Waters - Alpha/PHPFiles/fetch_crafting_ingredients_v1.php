<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = $mysqli->prepare("SELECT itmId, itmCategory, itmType
		FROM items 
		left join users on itmCategory = 6
		where users.usrId = ?");
	$query->bind_param("s", $usrId);
	$query -> execute();
	$query->bind_result($itmId, $itmCategory, $itmType);
	
	$result = $query->get_result();
	while ($row = $result->fetch_array(MYSQLI_NUM)) {
		printf ("[next]%s[space]%s[space]%s[space]", $row[0], $row[1], $row[2]);
	}
	$result->close();
	
	$mysqli -> close();
?>