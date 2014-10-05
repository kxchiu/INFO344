<?php
	if(!isset($_POST['input']))
	{
		exit;
	}

	$conn = new PDO('mysql:host=kcinstance.ctrqvpurnkfg.us-west-2.rds.amazonaws.com;dbname=NBA', 'kxchiu', 'Quanta01$');
	$name = '%'.$_POST['input'].'%';
	$stmt = $conn->prepare("SELECT * FROM Stats WHERE playername LIKE '$name'");
	$stmt->execute();
	
	$result = $stmt->fetchAll();

	echo json_encode($result);
?>
