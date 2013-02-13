<?php
	function get_update_info()
	{
		global $error_manager;

		$mysql = new mysql_helper('localhost', 0, 0);

		$results = $mysql->execute_query('SELECT * FROM `updates` ORDER BY Version DESC');

		if (!isset($results[0]['version']) || !isset($results[0]['change_log']))
			return $error_manager->error_generator(ERROR_SQL_QUERY_FAILED);
		if (isset($results['error_code']) && isset($results['error_description']))
			return $results;

		$latest_version = $results[0]['version'];
		$download_link = 'http://www.xboxchaos.com/assembly/update.zip';
		$output_array = array(
			'download_link' => $download_link,
			'latest_version' => $latest_version,
			'change_logs' => $results);

		return $output_array;
	}
?>