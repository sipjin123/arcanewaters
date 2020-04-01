<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = "SELECT itmId, itmCategory, itmType
		FROM items 
		left join users on itmCategory = 6
		where users.usrId = ?";
	
	if ($stmt = $mysqli->prepare($query)) {
		$stmt->bind_param("s", $usrId);
	    $stmt->execute();
		$stmt->bind_result($itmId, $itmCategory, $itmType);

		while ($stmt->fetch()) {
		printf ("[next]%s[space]%s[space]%s[space]", $itmId, $itmCategory, $itmType);
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	
	$mysqli -> close();
?>