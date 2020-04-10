<?php
	$port = $_GET['port'];
	$deviceName = $_GET['deviceName'];
	
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	// Set xml version
	$query = "SELECT openAreas, voyages, openAreas FROM arcane.server_status where svrDeviceName = ? and svrPort = ?;";
		
	if ($stmt = $mysqli->prepare($query)) {
		$stmt->bind_param("ss", $deviceName, $port);
	    $stmt->execute();
		$stmt->bind_result($openAreas, $voyages, $openAreas);

		while ($stmt->fetch()) {
			printf ("[next]%s[space]%s[space]%s", $openAreas, $voyages, $openAreas);
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
?>