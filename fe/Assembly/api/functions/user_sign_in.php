<?php
	function user_sign_in($data_array)
	{
		global $error_manager;

		$input_username = $data_array['username'];
		$input_password = $data_array['password'];
		$input_timestamp = time();
		$mysql = new mysql_helper('localhost', 0, 1);
		$bindings = array();
		$bindings = $mysql->add_binding($bindings, 's', $input_username);
		$members_result = $mysql->execute_query('SELECT * FROM `members` WHERE (`members_l_username` = ?0)', $bindings);
		$check_valid = $mysql->check_query_valid($members_result, array(
			'member_group_id',
			'member_id',
			'members_pass_salt',
			'members_pass_hash',
			'members_display_name',
			'posts'));
		if ($check_valid['error_code'] != -1)
			return $check_valid;

		$output_session_id = random_string_generation(16);
		$output_gid = $members_result[0]['member_group_id'];
		$output_mid = $members_result[0]['member_id'];
		$output_salt = $members_result[0]['members_pass_salt'];
		$output_hash = $members_result[0]['members_pass_hash'];
		$output_display_name = $members_result[0]['members_display_name'];
		$output_posts = $members_result[0]['posts'];
		$calculated_hash = md5(md5($output_salt) . $input_password);
		if ($calculated_hash != $output_hash)
			return $error_manager->error_generator(ERROR_INVALID_PASS);

		$bindings = array();
		$bindings = $mysql->add_binding($bindings, 'i', $output_mid);
		$avatar_result = $mysql->execute_query('SELECT * FROM `profile_portal` WHERE `pp_member_id` = ?0', $bindings);
		$check_valid = $mysql->check_query_valid($avatar_result, array('pp_member_id'));

		if ($check_valid['error_code'] != SUCCESS)
			return $check_valid;

		$output_avatar_raw = $avatar_result[0]['pp_thumb_photo'];
		if (substr($output_avatar_raw,0,3) != 'http')
			$output_avatar_raw = 'http://uploads.xbchaos.netdna-cdn.com/' . $output_avatar_raw;

		$bindings = array();
		$bindings = $mysql->add_binding($bindings, 'i', $output_mid);
		$assembly_users_result = $mysql->execute_query('SELECT * FROM `users` WHERE `member_id` = ?0', $bindings);
		$check_valid = $mysql->check_query_valid($assembly_users_result, array(
			'member_id',
			'session_id',
			'timestamp'));

		if ($check_valid['error_code'] != SUCCESS)
			return $check_valid;

		if ($check_valid['error_code'] == SEMIE_SQL_QUERY_ZERO_ROWS)
		{
			// mysql_query("INSERT INTO `users` (`member_id` , `session_id` , `timestamp`) VALUES ('$member_id' , '$session_id', '$timestamp')");
			$bindings = array();
			$bindings = $mysql->add_binding($bindings, 'i', $output_mid);
			$bindings = $mysql->add_binding($bindings, 's', $output_session_id);
			$bindings = $mysql->add_binding($bindings, 'i', $input_timestamp);
			$insert_result = $mysql->execute_query('INSERT INTO `users` (`member_id` , `session_id` , `timestamp`) VALUES (?0 , ?1, ?2)', $bindings);

			// $check_valid = $mysql->check_query_valid($insert_result, array());
			// if ($check_valid['error_code'] != SUCCESS)
			// 	return $check_valid;
		}
		else
		{
			$bindings = array();
			$bindings = $mysql->add_binding($bindings, 'i', $output_mid);
			$bindings = $mysql->add_binding($bindings, 's', $output_session_id);
			$bindings = $mysql->add_binding($bindings, 'i', $input_timestamp);
			$update_result = $mysql->execute_query('UPDATE `users` SET (`session_id` = ?0, `timestamp` = ?1) WHERE `users`.`member_id` = ?2', $bindings);

			// $check_valid = $mysql->check_query_valid($update_result, array());
			// if ($check_valid['error_code'] != SUCCESS)
			// 	return $check_valid;
		}

		$return_array = array(
			'member_id' => $output_mid,
			'session_id' => $output_session_id,
			'display_name' => $output_display_name,
			'signin_name' => $input_username,
			'group_id' => $output_gid,
			'post_count' => $output_posts,
			'avatar_url' => $output_avatar_raw
			);

		return $return_array;
	}
?>