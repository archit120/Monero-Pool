<?php

require 'Predis/Autoloader.php';

Predis\Autoloader::register();

$settings=json_decode(file_get_contents("config.json"), true);

Predis\Autoloader::register();
$client = new Predis\Client($settings);
$block = $client->zrange("Block",0,-1);

$jblock = array();

foreach ($block as $k)
 {
    $jblock[] = json_decode(json_encode($client->hgetall($k)), false);
	//var_dump(json_decode($v));
}    
$top50 = array();

$jblock = array_reverse($jblock);

$max = 50;
foreach($jblock as $jb)
{
	if($jb->Found == "true")
	{	$a = array();

		$a["Founder"] = json_decode($jb->Founder);
		$a["Height"] = json_decode($jb->BlockHeight);
		$top50[]=json_encode($a);
		$max--;
		if($max==0)
			break;
	}
}

echo(json_encode($top50));
?>