<?php

	$version = $_GET['version'];
	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	// Set xml version
	$query = "UPDATE xml_status SET version = ".$version." where id = 1";
		
	if ($stmt = $mysqli->prepare($query)) {
	    $stmt->execute();
		$stmt->close();
	} else {
		printf ("Failed to Query");
	}
	$mysqli -> close();
		
	/* Archive the entire folder */
	$pathdir = "xml_files/";
	$nameArchive = "archive_files/MyArchive_".$version.".zip";
	$zip = new ZipArchive;

	if ($zip -> open($nameArchive, ZipArchive::CREATE) === TRUE) {
		$dir = opendir($pathdir);
		
		while ($file = readdir($dir)) {
			if (is_file($pathdir.$file)) {
				$zip -> addFile($pathdir.$file, $file);
			}
		}
		
		$zip -> close();
		
		echo "Archive Success";
	} else {
		echo "Archive Fail";
	}
	
?>

