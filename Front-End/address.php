<?php
require 'Predis/Autoloader.php';
$settings=json_decode(file_get_contents("config.json"), true);

Predis\Autoloader::register();
$client = new Predis\Client($settings);

$jaddress = $client->zrange("Miner",0,-1);
$addresses = array();
foreach($jaddress as $j)
{
	$addresses[] = $client->hgetall($j);
}
$addresses = json_decode(json_encode($addresses), FALSE);


$address=array_filter($addresses, function($n)
	{
		return json_decode($n->Address, FALSE)==$_GET["address"];
	});
	
$address=array_shift($address);
if($address==NULL)
{
	echo "{\"found\":false}";
}
else
{
	$miners = count(json_decode($address->MinersWorker, true));
		
	$obj = json_decode($address->TimeHashRate, false);
	$obj = array_reverse(json_decode(json_encode($obj), true));
	$obj = array_shift($obj);
	
	$hashrate = $obj;
	$shares = 0;
	$blockreward = array_reverse(json_decode($address->BlockReward, true));
	$blockreward=json_decode(json_decode(json_encode($client->hgetall(array_shift($blockreward))), false)->Shares, false);
	
	foreach($blockreward as $rShare)
	{
		$shares += json_decode(json_decode(json_encode($client->hgetall($rShare)), false)->Value);
	}

	$blocks = array();
	$blockheaders = array();
	
	
	$max = 10;
	foreach(array_reverse(json_decode($address->BlockReward,true)) as $blockid)
	{
		$blockreward = json_decode(json_encode($client->hgetall($blockid), FALSE));
		$a = json_decode($blockreward->Block, FALSE);//->BlockHeight);
		$blocks[] = json_decode(json_encode($client->hgetall($a)) , false)->BlockHeight;
		$max--;
		if($max==0)
			break;
	}
	//var_dump($blocks);
	$response = array();
	$response["found"] = true;
	$response["miners"] = $miners;
	$response["hashrate"] = $hashrate;
	$response["roundshare"] = $shares;
	$response["totalpaidout"] =  json_decode($address->TotalPaidOut, true) / $settings["coinunits"];
	$response["blocks"] = $blocks;
	
	$labels = array();
	$data = array();
	
	$tz = new DateTimeZone('Europe/London');

	foreach(json_decode($address->TimeHashRate, true) as $k=>$v)
	{
		$datetime = new DateTime($k);
	    $datetime->setTimeZone($tz);
		$labels[] = $datetime->format('Y-m-d H:i:s');
		$data[] = $v;
	}
	
	$fixL = array_reverse(array_values($labels));
	$fixL = array_shift($fixL);
	
	$now = new DateTime();
	$now->setTimeZone($tz);
	$fixL = new DateTime($fixL);
	$seconds = $now->getTimestamp() - $fixL->getTimestamp();
	
	if($seconds/60 > 5)
	{
		$fixedL = $fixL;
		for($i=0; $i<$seconds/(60 * 5); $i++)
		{
			$labels[] = $fixedL->format('Y-m-d H:i:s');
			$data[] = 0;
			$fixedL->add(new DateInterval('PT' . '5' . 'M'));
		}
	}
	
	$finalFix = array();
	$length = count($labels);
	for($i = 0;$i<$length;$i++)
	{
		if($i==0 || $i == ($length - 1))
		{
			$labels[$i] = $labels[$i];
		}
		else
		{
			$labels[$i] = "";
		}
	}
	
	$response["labels"]= $labels;
	$response["data"]=$data;
	echo(json_encode($response));
}
?>