#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform float uShininess;

uniform sampler2D uTexture;
		
in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;
in vec2 outTexture;

void main()
{
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * uLightColor;

    float diffuseStrength = 0.3;
    vec3 norm = normalize(outNormal);
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uLightColor * diffuseStrength;

    float specularStrength = 0.6;
    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    float spec = sign(max(dot(outNormal, lightDir),0)) * pow( max(dot(viewDir, reflectDir), 0.0), uShininess) / max(max(dot(norm,viewDir), 0), max(dot(norm,lightDir), 0));

    // texture color
    vec4 textureColor = texture(uTexture, outTexture);

    vec3 result = (ambient + diffuse + spec) * outCol.rgb + textureColor.rgb;

    FragColor = vec4(result, outCol.w);
}