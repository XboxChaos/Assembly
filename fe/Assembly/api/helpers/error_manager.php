<?php
	class error_manager
	{
		function get_friendly_error_message($errorCode)
		{
			switch ($errorCode) 
			{
				case -1:
					return "Success";

				case 0:
					return "That username doesn't exist. Are you sure you're using your login name from XboxChaos?";

				case 1:
					return "Your password is incorrect.";

				case 2:
					return "There was a problem contacting the Database. Please try again later.";

				case 3:
					return "There was a problem executing a MySQL query to the database.";

				case 4:
					return "Your usergroup was not allowed to sign in. Are you sure you have permissions to use this feature?";

				case 5:
					return "Your account has been locked out for 10 minutes for too many incorrect sign's. Please wait 10 minutes.";

				case 6:
					return "The requested action was not recognized.";

				case 7:
					return "The requested cache type does not exist in the database.";

				case 8:
					return "The SQL Row returned 0 rows.";


				default:
					return "Invalid error identification code.";
			}
		}

		function error_generator($errorCode)
		{
			return array(	"error_code" => $errorCode,
							"error_description" => $this->get_friendly_error_message($errorCode));
		}
	}

	define("SUCCESS", -1);
	define("ERROR_INVALID_USER", 0);
	define("ERROR_INVALID_PASS", 1);
	define("ERROR_SERVER_REFUSED", 2);
	define("ERROR_SQL_QUERY_FAILED", 3);
	define("ERROR_GROUP_NOT_ALLOWED",4);
	define("ERROR_TOO_MANY_ATTEMPTS",5);
	define("ERROR_INVALID_ACTION",6);
	define("ERROR_CACHE_TYPE_DOES_NOT_EXIST",7);
	define("SEMIE_SQL_QUERY_ZERO_ROWS", 8);
?>