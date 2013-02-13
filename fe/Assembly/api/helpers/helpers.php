<?php
	function random_string_generation($length)
	{
		$randomString = "";
		for ($i = 0; $i < $length; $i++)
		{
			$character = chr(rand(48,122));
			$randomString = $randomString . $character;
		}
		return $randomString;
	}
?>