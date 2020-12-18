grammar BS_Reach;

hsc : (globalDeclaration|scriptDeclaration)* ;

globalDeclaration : LP 'global' VALUETYPE ID expression RP ;

scriptDeclaration : LP 'script' SCRIPTTYPE VALUETYPE scriptID scriptParameters? expression+ RP ;

scriptParameters : LP parameter (',' parameter)* RP ;

cond : LP 'cond' condGroup+ RP ;

branch : LP 'branch' expression* RP ;

call : LP callID expression* RP ;

condGroup : LP expression expression+ RP ;

parameter: VALUETYPE ID ;

scriptID 
        :       ID
        |       INT
        ;

callID 
        :       scriptID
        |       VALUETYPE
        ;

expression    
        :       literal 
        |       call               
        |       branch 
        |       cond
        ;

literal 
        :       INT
        |       FLOAT
        |       STRING
        |       DAMAGEREGION
        |       MODELSTATE
        |       BOOLEAN
        |       ID
        |       NONE
        |       VALUETYPE       // The value type player causes problems
        ;


// Lexers
//--------------------------------------------------------------------

BOOLEAN  
        : 'true' 
        | 'false' 
        | 'True' 
        | 'False' 
        | 'TRUE' 
        | 'FALSE'
        ;

NONE: 'none' | 'None' | 'NONE';
		
DAMAGEREGION
        :       'gut'
        |       'chest'
        |       'head'
        |       'left shoulder'
        |       'left arm'
        |       'left leg'
        |       'left foot'
        |       'right shoulder'
        |       'right arm'
        |       'right leg'
        |       'right foot'
        ;
		
MODELSTATE
        :       'standard'
        |       'minor damage'
        |       'medium damage'
        |       'major damage'
        |       'destroyed'
        ;   

VALUETYPE
        :       'actor_type'
        |       'ai'
        |       'ai_behavior'
        |       'ai_command_list'
        |       'ai_command_script'
        |       'ai_line'
        |       'ai_orders'
        |       'animation_budget_reference'
        |       'animation_graph'
        |       'any_tag'
        |       'any_tag_not_resolving'
        |       'bink_definition'
        |       'bitmap'
        |       'boolean'
        |       'button_preset'
        |       'character_physics'
        |       'cinematic_definition'
        |       'cinematic_lightprobe'
        |       'cinematic_scene_definition'
        |       'cinematic_transition_definition'
        |       'controller'
        |       'conversation'
        |       'cui_screen_definition'
        |       'cutscene_camera_point'
        |       'cutscene_flag'
        |       'cutscene_recording'
        |       'cutscene_title'
        |       'damage'
        |       'damage_effect'
        |       'damage_region'
        |       'designer_zone'
        |       'device'
        |       'device_group'
        |       'device_name'
        |       'effect'
        |       'effect_scenery'
        |       'effect_scenery_name'
        |       'event'
        |       'firing_point_evaluator'
        |       'folder'
        |       'function_name'
        |       'game_difficulty'
        |       'hud_corner'
        |       'hud_message'
        |       'joystick_preset'
        |       'lightmap_definition'
        |       'long'
        |       'looping_sound'
        |       'looping_sound_budget_reference'
        |       'model_state'
        |       'mp_team'
        |       'navpoint'
        |       'network_event'
        |       'object'
        |       'object_definition'
        |       'object_list'
        |       'object_name'
        |       'passthrough'
        |       'player'
        |       'player_character_type'
        |       'player_color'
        |       'player_model_choice'
        |       'point_reference'
        |       'primary_skull'
        |       'real'
        |       'render_model'
        |       'scenery'
        |       'scenery_name'
        |       'script'
        |       'secondary_skull'
        |       'shader'
        |       'short'
        |       'skull'
        |       'sound'
        |       'sound_budget_reference'
        |       'special_form'
        |       'starting_profile'
        |       'string'
        |       'string_id'
        |       'structure_bsp'
        |       'structure_definition'
        |       'style'
        |       'subtitle_setting'
        |       'team'
        |       'trigger_volume'
        |       'unit'
        |       'unit_name'
        |       'unit_seat_mapping'
        |       'unparsed'
        |       'vehicle'
        |       'vehicle_name'
        |       'voice_mask'
        |       'voice_output_setting'
        |       'void'
        |       'weapon'
        |       'weapon_name'
        |       'zone_set'
        ;

SCRIPTTYPE 
        :   'startup'
        |   'dormant'
        |   'continuous'
        |   'static'
        |   'command_script'
        |   'stub'
        ;
     

STRING : '"' STRINGCHARACTER* '"' ;

FLOAT : '-'? DIGIT+ '.' DIGIT+ ;

INT : '-'? DIGIT+ ;

LP : '(' ;
RP : ')' ;

ID : (LCASE|UCASE|DIGIT|SPECIAL)+ ;

fragment
STRINGCHARACTER
	:	~["\r\n]
	|	ESCAPESEQUENCE
	;

fragment
ESCAPESEQUENCE : '\\' [btnfr"'\\] ;

fragment
LCASE : [a-z] ;
fragment
UCASE : [A-Z];

fragment
DIGIT : [0-9] ;

fragment
SPECIAL 
        :	'_'
        |       '-'
        |	'#'
        |	':'
        |	'%'
        |	'?'
        |	'*'
        |	'!'
        |	'\\'
        |	'/'
        |	'|'
        |	'$'
        |	'.'
        |	'+'
        |	'@'
        |	'['
        |	']'
        |	'\''
        |	'`'
        |	'='
        |	'<'
        |	'>'
        ;


// Discard
//--------------------------------------------------------------------

WS : [ \t\r\n]+ -> skip ;
BLOCKCOMMENT: ';*' .*? '*;' -> skip;
COMMENT: ';' .*? [\r\n]+ -> skip;


