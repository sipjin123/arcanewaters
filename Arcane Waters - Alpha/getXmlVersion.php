<?php
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	// Fetch all sea monsters info and save to text file
	$query = "SELECT version
		FROM xml_status where id = 1";
	if ($stmt = $mysqli->prepare($query)) {
	    $stmt->execute();
		$stmt->bind_result($version);

		while ($stmt->fetch()) {
			echo $version;
		}
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	$mysqli -> close();
?>

