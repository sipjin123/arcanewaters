<?php
	$id = $_GET['id'];

	$file = "archive_files/MyArchive_".$id.".zip";
	header('Content-type: application/x-download');
	header('Content-Disposition: attachment; filename="'.$file.'"');
	header('Content-Length: '.filesize($file));
	readfile($file);
?>
