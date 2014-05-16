<?php

require 'Predis/Autoloader.php';

Predis\Autoloader::register();

$client = new Predis\Client();

$address = $client->hgetall("miners");

$jaddress = NULL;

foreach ($address as $k => $v) {
    $add = json_decode($v, false);
	
	if($add->Address==$_GET["address"])
	{
		$jaddress=$add;
		break;
	}
	//var_dump(json_decode($v));
}    

if($jaddress==NULL)
{
	echo "{\"found\":false}";
}
else
{
	$miners = count($jaddress->MinersWorker);
	$hashrate = $jaddress->HashRate;
	$reward="To be implemented";
	$blockrewards = $client->hgetall("blockrewards");
	$rblocks = $client->hgetall("blocks");
	$blocks = array();
	
	$max = 10;
	foreach($jaddress->BlockReward as $blockid)
	{
		$blockreward = json_decode($blockrewards[$blockid]);
		$blocks[] = json_decode($rblocks[$blockreward->Block])->BlockHeight;
		$max--;
		if($max==0)
			break;
	}
	$response = array();
	$response["found"] = true;
	$response["miners"] = $miners;
	$response["hashrate"] = $hashrate;
	$response["reward"] = $reward;
	$response["blocks"] = $blocks;
	
	echo(json_encode($response));
}
?>