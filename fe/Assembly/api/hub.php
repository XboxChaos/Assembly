<?php
	define('IN_API', TRUE);
	define('IN_DEV', FALSE);

	error_reporting(E_ERROR | E_WARNING | E_PARSE | E_NOTICE);

	// Helpers
	include_once 'helpers/error_manager.php';
	include_once 'helpers/mysql_helper.php';
	include_once 'helpers/helpers.php';

	// Storage
	include_once 'data_vault.php';

	// Functions
	include_once 'functions/cache_data.php';
	include_once 'functions/update_data.php';
	include_once 'functions/user_sign_in.php';

	$action = '';
	$incoming_data = array();
	$error_manager = new error_manager;

	if (IN_DEV)
	{
		if (isset($_GET['json']))
		{
			$programIn = $_GET['json'];
			$incoming_data = json_decode($programIn, TRUE);
		}
		$action = $_GET['action'];
	}
	else
	{
		$programIn = file_get_contents('php://input');
		$incoming_data = json_decode($programIn, TRUE);
		$action = $incoming_data['action'];
	}

	$outgoing_data = array();
	switch($action)
	{
		case 'update':
		case 'update_asm':
			$outgoing_data = get_update_info();
			break;

		case 'sign_in':
			$outgoing_data = user_sign_in($incoming_data);
			break;

		case 'cache_data':
			$outgoing_data = get_cached_data($incoming_data);
			break;

		// case 'memory_game_list':
		// 	$outgoing_data = get_memory_game_list($incoming_data);
		// 	break;

		// case 'test':
		// 	$outgoing_data['foo'] = 'bar';
		// 	break;

		default:
		 	$outgoing_data = $error_manager->error_generator(ERROR_INVALID_ACTION);
		 	break;
	}

	//Indicate success if no error info was stored
	if (!isset($outgoing_data['error_code']) || !isset($outgoing_data['error_description']))
	{
		$outgoing_data['error_code'] = -1;
		$outgoing_data['error_description'] = 'Success';
	}

	$outgoing_data['timestamp'] = time();
	$stringArray = json_encode($outgoing_data);
	printf($stringArray);
?>