<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = $mysqli->prepare("SELECT itmId, itmCategory, itmType, 
		CASE 
			WHEN itmCategory = 1 THEN arcane.equipment_weapon_xml_v3.xmlContent
			WHEN itmCategory = 2 THEN arcane.equipment_armor_xml_v3.xmlContent
		END AS equipmentXML
		FROM arcane.items 
		left join arcane.equipment_weapon_xml_v3 on (itmCategory = 1 and itmType = arcane.equipment_weapon_xml_v3.equipmentTypeID)
		left join arcane.equipment_armor_xml_v3 on (itmCategory = 2 and itmType = arcane.equipment_armor_xml_v3.equipmentTypeID)
		left join arcane.users on armId = itmId or wpnId = itmId
		where (armId = itmId or wpnId = itmId) and items.usrId = ?");
		
	$query->bind_param("s", $usrId);
	$query -> execute();
	$query->bind_result($itmId, $itmCategory, $itmType, $equipmentXML);
	
	$result = $query->get_result();
	while ($row = $result->fetch_row()) {
		printf ("[next]%s[space]%s[space]%s[space]%s[space]", $row[0], $row[1], $row[2], $row[3]);
	}
	$result->close();
	
	$mysqli -> close();
?>