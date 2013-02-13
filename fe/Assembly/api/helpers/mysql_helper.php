<?php
	/// <summary>
	/// this only works on PHP 5.0+, yolo
	/// </summary>
	class mysql_helper
	{
		function __construct($host, $accountId, $databaseNameId)
		{
			global $password_data, $database_data, $error_manager;

			$this->_host = $host;
			$this->_username = $password_data[$accountId]['username'];
			$this->_password = $password_data[$accountId]['password'];
			$this->_database_name = $database_data[$databaseNameId];

			$this->_db = new mysqli($this->_host, $this->_username, $this->_password, $this->_database_name);
			$this->_is_connected = !(mysqli_connect_errno());
		}

		function execute_query($queryText, $binding_content = array())
		{
			global $password_data, $database_data, $error_manager;
			
			$this->connect();

			$query = $queryText;
			for($i = 0; $i < count($binding_content); $i++)
			{
				$bindingType = $binding_content[$i]['type'];
				$bindingData = $this->_db->real_escape_string($binding_content[$i]['data']);

				switch($bindingType)
				{
					case 'i':
						$query = str_replace('?' . $i, $bindingData, $query);
						break;

					case 's':
					default:
						$query = str_replace('?' . $i, '\'' . $bindingData . '\'', $query);
						break;
				}
			}
			if (!$this->_db->real_query($query))
				return $error_manager->error_generator(ERROR_SQL_QUERY_FAILED);

			$raw_result = $this->_db->store_result();
			$num_rows = $raw_result->num_rows;
			$results = array();
			for($i = 0; $i < $num_rows; $i++)
			{
				$raw_result->data_seek($i);
				array_push($results, $raw_result->fetch_assoc());
			}
			$this->close();
			return $results;
		}
		function escape_real_string($input)
		{
			$this->connect();

			return $this->_db->real_escape_string($input);
		}

		function connect()
		{
			global $password_data, $database_data, $error_manager;

			$this->_db = new mysqli($this->_host, $this->_username, $this->_password, $this->_database_name);
			if (mysqli_connect_errno())
			{
				$this->_is_connected = false;
				return $error_manager->error_generator(ERROR_SERVER_REFUSED);
			}
			else
				$this->_is_connected = true;
		}
		function close()
		{
			if ($this->_is_connected)
				$this->_db->close();

			$this->_is_connected = false;
		}


		// Post-Query Exception Checker
		function check_query_valid($sql_results, $mandatory_fields = array())
		{
			global $error_manager;

			if (isset($sql_results['error_code']) && isset($sql_results['error_description']))
				return $sql_results;
			if (count($sql_results) == 0)
				return $error_manager->error_generator(SEMIE_SQL_QUERY_ZERO_ROWS);
			if ($sql_results == null)
				return $error_manager->error_generator(ERROR_SQL_QUERY_FAILED);

			$valid_result = count(
				array_intersect(
					$mandatory_fields, 
					$sql_results[0]) == count($mandatory_fields));

			if ($valid_result)
				return $error_manager->error_generator(SUCCESS);
			else
				return $error_manager->error_generator(ERROR_SQL_QUERY_FAILED);
		}

		// Binding Functions
		function add_binding($current_bindings, $type, $data)
		{
			$current_bindings[] = 
				array(
					'type' => $type, 
					'data' => $data
					);

			return $current_bindings;
		}

		var $_host;
		var $_username;
		var $_password;
		var $_database_name;
		var $_db;
		var $_is_connected;
	}
?>