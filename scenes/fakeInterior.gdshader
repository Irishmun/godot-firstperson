shader_type spatial;

uniform vec2 rooms = vec2(1,1);
uniform float room_seed :hint_range(0.0, 999.0, 1.0)= 0.0;
uniform vec2 atlas_rooms = vec2(1,1);
uniform float depth : hint_range(0.001, 0.999)= 0.5;
uniform float emission_cutoff : hint_range(0.0, 1.0)= 0.5;
uniform float emission_strength :hint_range(0.0, 10.0)= 1.0;
uniform sampler2D room_texture :hint_default_white;
uniform sampler2D room_emit_texture :hint_default_black;
uniform sampler2D emission_texture :hint_default_black;
uniform sampler2D window_texture :hint_default_transparent;

varying vec3 tangent_view_dir;

//pseudo random 
vec2 f_random(float co){
	return fract(sin((co + room_seed) * vec2(12.9898, 78.233)) * 43758.5453);
}

vec4 screen(vec4 base, vec4 blend){
	return 1.0 - (1.0 - base) * (1.0 - blend);
}


void vertex() {
	//scale uv by room count
	UV = UV * rooms;
	//get camera pos in world coords
	vec3 cam_pos = (inverse(MODELVIEW_MATRIX) * vec4(0,0,0,1)).xyz;//object space
	vec3 view_dir = VERTEX - cam_pos;
	vec3 bitangent = normalize(cross(TANGENT, NORMAL));
	//get tangent space camera vector
	tangent_view_dir = vec3(
		dot(view_dir, TANGENT),
		dot(view_dir, bitangent),
		dot(view_dir, NORMAL)
	);	
}

void fragment() {
	//room uvs
	vec2 room_uv = fract(UV);
	vec2 room_index_uv = floor(UV);
	//randomize rooms
	vec2 n = floor(f_random(room_index_uv.x + room_index_uv.y * (room_index_uv.x + 1.0)) * atlas_rooms);
	room_index_uv += n;
	//get room depth from atlas, use depth otherwise
	float far_frac = textureLod(room_texture, (room_index_uv + 0.5) / atlas_rooms, 0.0).a;
	if(far_frac == 1.0) far_frac = depth;
	
	float depth_scale = 1.0 / (1.0 - far_frac) - 1.0;
	//trace box from view dir
	vec3 pos = vec3(room_uv * 2.0 - 1.0, -1.0);
	vec3 _tangent_view_dir = tangent_view_dir;
	_tangent_view_dir.z *= -depth_scale;
	vec3 id = 1.0 / _tangent_view_dir;
	vec3 k = abs(id) - pos * id;
	float k_min = min(min(k.x, k.y), k.z);
	pos += k_min * _tangent_view_dir;
	//0.0 - 1.0 room depth
	float interp = pos.z * 0.5 + 0.5;
	//room perspective in texture (assumes camera with fov of 53.13 (atan(0.5)))
	float real_z = clamp(interp, 0.0, 1.0) / depth_scale + 1.0;
	interp = 1.0 - (1.0 / real_z);
	interp *= depth_scale + 1.0;
	//interpolate from back to near wall
	vec2 interior_uv = pos.xy * mix(1.0, far_frac,interp);
	interior_uv = interior_uv * 0.5 + 0.5;
	//sample room texture
	vec2 uv = (room_index_uv+interior_uv) / atlas_rooms;
	vec4 room = textureLod(room_texture, uv, 0.0).rgba;
	vec3 emit = textureLod(room_emit_texture, uv, 0.0).rgb;
	//emission based on cutoff
	ivec2 emission_texture_size = textureSize(emission_texture, 0);
	//int x = int(float(UV.x * float(emission_texture_size.x))) % int(rooms.x);
	//int y = int(float(UV.y * float(emission_texture_size.y))) % int(rooms.y);
	//scale UV across whole surface, offset by half room
	vec2 stretched_uv = (UV / rooms) - (0.5 / rooms);
	//force UV pixelation
	vec2 pixelated_uv = round(stretched_uv * rooms) / rooms;
	float is_emit = dot(textureLod(emission_texture, pixelated_uv, 0.0).rgb, vec3(0.299, 0.587, 0.114));
	is_emit = is_emit >= emission_cutoff ? 1.0 : 0.0;
	//get window texture for mixing	
	vec4 window =textureLod(window_texture, UV, 0.0);
	//apply final result
	//ALBEDO = screen(room * (1.0 - is_emit),textureLod(window_texture, UV, 0.0).rgba).rgb;
	//ALBEDO = room * (1.0 - is_emit);
	ALBEDO = mix((room * (1.0 - is_emit)).rgb, window.rgb, window.a);
	EMISSION = emit * is_emit * emission_strength;
}


//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
