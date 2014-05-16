<?php

require 'Predis/Autoloader.php';

Predis\Autoloader::register();

$client = new Predis\Client();

$address = $client->hgetall("miners");

$jaddress = array();

foreach ($address as $k => $v) {
    $jaddress[] = json_decode($v, false);
	//var_dump(json_decode($v));
}    
$top10 = array();

usort($jaddress, function($a, $b) { return $b->HashRate - $a->HashRate; });

$max = 10;
foreach($jaddress as $ja)
{
	$top10[$ja->Address]=$ja->HashRate;
	$max--;
	if($max==0)
		break;
}

echo(json_encode($top10));
?>