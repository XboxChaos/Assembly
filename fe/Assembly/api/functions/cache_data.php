<?php
	function get_cached_data($data_array)
	{
		global $error_manager;

		$requestedTimestamp = $data_array['timestamp'];
		$requestedType = $data_array['type'];

		$mysql = new mysql_helper('localhost', 0, 0);

		$bindings = array();
		$bindings = $mysql->add_binding($bindings, 's', $requestedType);
		$results = $mysql->execute_query('SELECT * FROM `cache` WHERE (`type` = ?0)', $bindings);

		if (!isset($results[0]['type']) || !isset($results[0]['timestamp']))
			return $error_manager->error_generator(ERROR_SQL_QUERY_FAILED);
		if (isset($results['error_code']) && isset($results['error_code']))
			return $results;

		$responseType = $results[0]['type'];
		$responseTimestamp = $results[0]['timestamp'];
		$returnArray = array('update_cache' => ($responseTimestamp > $requestedTimestamp));
		
		return $returnArray;
	}
?>