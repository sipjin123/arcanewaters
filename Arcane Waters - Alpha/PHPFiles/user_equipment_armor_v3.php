<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = "SELECT xmlContent, itmId FROM items left join equipment_armor_xml_v3 on itmType = xml_id where itmCategory = 2 and usrId = ?";
	
	if ($stmt = $mysqli->prepare($query)) {
	
		$stmt->bind_param("s", $usrId);
	    $stmt->execute();
		$stmt->bind_result($xmlContent, $itmId);

		while ($stmt->fetch()) {
			printf ("%s:::%s:::", $xmlContent, $itmId);
		}
		$stmt->close();
	}
	
	$mysqli -> close();
?>
