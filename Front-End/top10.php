<?php

require 'Predis/Autoloader.php';

Predis\Autoloader::register();

$settings=json_decode(file_get_contents("config.json"), true);

Predis\Autoloader::register();
$client = new Predis\Client($settings);
		
$address = $client->zrange("Miner",0,-1);

$jaddress = array();

foreach ($address as $k)
 {
    $jaddress[] = json_decode(json_encode($client->hgetall($k)), false);
	//var_dump(json_decode($v));
}    
$top10 = array();

usort($jaddress, function($a, $b) { 
	$obj = json_decode($a->TimeHashRate, false);
	$obj = array_reverse(json_decode(json_encode($obj), true));
	$obj = array_shift($obj);
	
	$obj2 = json_decode($b->TimeHashRate, false);
	$obj2 = array_reverse(json_decode(json_encode($obj2), true));
	$obj2 = array_shift($obj2);
	return $b->HashRate - $a->HashRate; 
});

$max = 10;
foreach($jaddress as $ja)
{
	$obj = json_decode($ja->TimeHashRate, false);
	$obj = array_reverse(json_decode(json_encode($obj), true));
	$obj = array_shift($obj);
	
	$top10[ json_decode($ja->Address)]=$obj;
	$max--;
	if($max==0)
		break;
}

echo(json_encode($top10));
?>