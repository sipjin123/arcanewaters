<?php
	$usrId = $_GET['usrId'];
	$steamAppid = '1266340';
	$publisherApiKey = '16FBA4602CFF4C139DC40E01D58F8869';
	
	$url = 'https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?key='.$publisherApiKey.'&steamid='.$usrId.'&appid='.$steamAppid;
	
	//Use file_get_contents to GET the URL in question.
	$contents = file_get_contents($url);

	//If $contents is not a boolean FALSE value.
	if($contents !== false){
		//Print out the contents.
		echo $contents;
	}
?>