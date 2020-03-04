<?php
	$usrId = $_GET['usrId'];

	$mysqli = new mysqli("52.72.202.104", "test_user", "test_password", "arcane");

	/* check connection */
	if (mysqli_connect_errno()) {
		printf("Connect failed: %s\n", mysqli_connect_error());
		exit();
	}
	
	$query = $mysqli->prepare("SELECT itmId, itmCategory, itmType, crafting_xml_v2.xmlContent as craftingXML, equipment_weapon_xml_v3.xmlContent as equipmentXML FROM items 
		left join crafting_xml_v2 on (itmType like '100%' and REPLACE(itmType,'100','') = equipmentTypeID and crafting_xml_v2.equipmentCategory = 1) 
		left join equipment_weapon_xml_v3 on (itmCategory = 7 and itmType like '100%' and REPLACE(itmType,'100','') = equipment_weapon_xml_v3.equipmentTypeID)
		where (itmCategory = 7 and itmType like '100%') and items.usrId = ?");
		
	$query->bind_param("s", $usrId);
	$query -> execute();
	$query->bind_result($itmId, $itmCategory, $itmType, $craftingXML, $equipmentXML);
	
	$result = $query->get_result();
	while ($row = $result->fetch_row()) {
		printf ("[next]%s[space]%s[space]%s[space]%s[space]%s[space]", $row[0], $row[1], $row[2], $row[3], $row[4]);
	}
	$result->close();
	
	$mysqli -> close();
?>