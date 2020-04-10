<?php
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	// Set xml version
	$query = "SELECT svrPort, srvAddress, svrDeviceId, updateTime FROM server_status;";
		
	if ($stmt = $mysqli->prepare($query)) {
		/*$stmt->bind_param("s", $usrId);*/
	    $stmt->execute();
		$stmt->bind_result($svrPort, $srvAddress, $svrDeviceId, $updateTime);

		while ($stmt->fetch()) {
			printf ("[next]%s[space]%s[space]%s[space]%s", $svrPort, $srvAddress, $svrDeviceId, $updateTime);
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
?>

