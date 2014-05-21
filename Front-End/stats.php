<?php

require 'Predis/Autoloader.php';

Predis\Autoloader::register();

$settings=json_decode(file_get_contents("config.json"), true);

Predis\Autoloader::register();
$client = new Predis\Client($settings);
		
$stats = $client->hgetall("PoolInformation");
echo json_encode($stats);
		?>