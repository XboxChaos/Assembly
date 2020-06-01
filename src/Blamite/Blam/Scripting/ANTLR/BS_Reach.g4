grammar BS_Reach;

hsc : (gloDecl|scriDecl)+ ;

gloDecl : LP 'global' VALUETYPE ID expr RP ;

scriDecl : LP 'script' SCRIPTTYPE retType scriptID scriptParams? (call | gloRef | branch | cond)+ RP ;

scriptParams : LP VALUETYPE ID (',' VALUETYPE ID)* RP ;

cond : LP 'cond' condGroup+ RP ;

branch : LP 'branch' expr* RP ;

call : LP funcID expr* RP ;

condGroup : LP expr expr RP ;

scriptID 
        :       ID
        |       INT
        ;

funcID 
        :       '!='
        |       '>='
        |       '<='
        |       '*'
        |       '+'
        |       '<'
        |       '-'
        |       '='
        |       '>'
        |       ID
        |       VALUETYPE                       //messy
        ;
gloRef  :       ID; 

expr    :   lit | call | branch | cond;

retType : 'void' | VALUETYPE ;

lit 
        :       INT
        |       FLOAT
        |       STRING
        |       DAMAGEREGION
        |       MODELSTATE
        |       BOOLIT
        |       ID
        |       VALUETYPE                       //messy
        ;
		
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

BOOLIT  : 'true' | 'false' ;

VALUETYPE
        :       'unparsed'
        |       'special_form'
        |       'function_name'
        |       'passthrough'
        |       'void'
        |       'boolean'
        |       'real'
        |       'short'
        |       'long'
        |       'string'
        |       'script'
        |       'string_id'
        |       'unit_seat_mapping'
        |       'trigger_volume'
        |       'cutscene_flag'
        |       'cutscene_camera_point'
        |       'cutscene_title'
        |       'cutscene_recording'
        |       'device_group'
        |       'ai'
        |       'ai_command_list'
        |       'ai_command_script'
        |       'ai_behavior'
        |       'ai_orders'
        |       'ai_line'
        |       'starting_profile'
        |       'conversation'
        |       'player'
        |       'zone_set'
        |       'designer_zone'
        |       'point_reference'
        |       'style'
        |       'object_list'
        |       'folder'
        |       'sound'
        |       'effect'
        |       'damage'
        |       'looping_sound'
        |       'animation_graph'
        |       'damage_effect'
        |       'object_definition'
        |       'bitmap'
        |       'shader'
        |       'render_model'
        |       'structure_definition'
        |       'lightmap_definition'
        |       'cinematic_definition'
        |       'cinematic_scene_definition'
        |       'cinematic_transition_definition'
        |       'bink_definition'
        |       'cui_screen_definition'
        |       'any_tag'
        |       'any_tag_not_resolving'
        |       'game_difficulty'
        |       'team'
        |       'mp_team'
        |       'controller'
        |       'button_preset'
        |       'joystick_preset'
        |       'player_color'
        |       'player_model_choice'
        |       'voice_output_setting'
        |       'voice_mask'
        |       'subtitle_setting'
        |       'actor_type'
        |       'model_state'
        |       'event'
        |       'character_physics'
        |       'skull'
        |       'firing_point_evaluator'
        |       'damage_region'
        |       'object'
        |       'unit'
        |       'vehicle'
        |       'weapon'
        |       'device'
        |       'scenery'
        |       'effect_scenery'
        |       'object_name'
        |       'unit_name'
        |       'vehicle_name'
        |       'weapon_name'
        |       'device_name'
        |       'scenery_name'
        |       'effect_scenery_name'
        |       'cinematic_lightprobe'
        |       'animation_budget_reference'
        |       'looping_sound_budget_reference'
        |       'sound_budget_reference'
        ;

SCRIPTTYPE 
    :   'startup'
    |   'dormant'
    |   'continuous'
    |   'static'
    |   'command_script'
    |   'stub'
    ;
     


STRING : '"' .*? '"' ;

FLOAT : '-'? DIGIT+ '.' DIGIT+ ;


INT : '-'? DIGIT+ ;
ID : (LCASE|DIGIT|SPECIAL)+ ;

LP : '(' ;
RP : ')' ;

fragment
LCASE : [a-z] ;

fragment
DIGIT : [0-9] ;

fragment
SPECIAL 
        :  '!' 
        | '#' 
        | '$' 
        | '%' 
        | '*' 
        | '+' 
        | '.' 
        | '/' 
        | ':' 
        | '?' 
        | '@' 
        | '[' 
        | ']' 
        | '_' 
        |  '|'
        ;



COMMENT : ';' .*? '\n' -> skip;

WS : [ \t\r\n]+ -> skip ;