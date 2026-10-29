#include "Mesh.h"

void Mesh::init(Vertex* vertices, unsigned int numVertices, unsigned int* indices, unsigned int numIndices)
{
	IndexedModel model;

	initModel(model);
}

void Mesh::initModel(const IndexedModel& model)
{
	std::vector<Vertex> vertices(model.positions.size());

	for (unsigned int i = 0; i < vertices.size(); i++)
	{
		vertices[i].position = model.positions[i];
		vertices[i].texCoord = model.texCoords[i];
		vertices[i].normal = model.normals[i];
	}

	drawCount = model.indices.size();

	glCreateVertexArrays(1, &vao); //generate a vertex array and store it in the VAO

	glCreateBuffers(1, &vbo); 
	glCreateBuffers(1, &ebo); 

	glNamedBufferStorage(vbo, vertices.size() * sizeof(Vertex), vertices.data(), 0); //move the data to the GPU - type of data, size of data, starting address (pointer) of data, where do we store the data on the GPU (determined by type)
	glNamedBufferStorage(ebo, model.indices.size() * sizeof(unsigned int), model.indices.data(), 0);


	glCreateVertexArrays(1, &vao);
	glVertexArrayVertexBuffer(vao, 0, vbo, 0, sizeof(Vertex)); // binding slot 0, stride 32
	glVertexArrayElementBuffer(vao, ebo);

	glEnableVertexArrayAttrib(vao, 0);
	glVertexArrayAttribFormat(vao, 0, 3, GL_FLOAT, GL_FALSE, offsetof(Vertex, position));
	glVertexArrayAttribBinding(vao, 0, 0);

	glEnableVertexArrayAttrib(vao, 1);
	glVertexArrayAttribFormat(vao, 1, 2, GL_FLOAT, GL_FALSE, offsetof(Vertex, texCoord));
	glVertexArrayAttribBinding(vao, 1, 0);

	glEnableVertexArrayAttrib(vao, 2);
	glVertexArrayAttribFormat(vao, 2, 3, GL_FLOAT, GL_FALSE, offsetof(Vertex, normal));
	glVertexArrayAttribBinding(vao, 2, 0);
}

void Mesh::loadModel(const std::string& filename)
{
	IndexedModel model = OBJModel(filename).ToIndexedModel();
	initModel(model);
	Sphere meshSphere();
}

Mesh::~Mesh()
{
	glDeleteVertexArrays(1, &vertexArrayObject); // delete arrays
	glDeleteBuffers(1, &vbo); 
	glDeleteBuffers(1, &ebo);
}

Mesh::Mesh()
{
	drawCount = NULL;
}
void Mesh::draw()
{
	glBindVertexArray(vao);

	glDrawElements(GL_TRIANGLES, drawCount, GL_UNSIGNED_INT, 0);
	//glDrawArrays(GL_TRIANGLES, 0, drawCount);

	glBindVertexArray(0);
}

void Mesh::updateSphereData(glm::vec3 pos, float radius)
{
	meshSphere.SetPos(pos);
	meshSphere.SetRadius(radius);
}


