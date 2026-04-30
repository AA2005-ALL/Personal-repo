#include "Mesh.h"
#include <vector>


void Mesh::init(Vertex* vertices, unsigned int numVertices, unsigned int* indices, unsigned int numIndices)
{
	IndexedModel model;

	for (unsigned int i = 0; i < numVertices; i++)
	{
		model.positions.push_back(*vertices[i].GetPos());
		model.texCoords.push_back(*vertices[i].GetTexCoord());
	}

	for (unsigned int i = 0; i < numIndices; i++)
		model.indices.push_back(indices[i]);

	initModel(model);
}

void Mesh::initModel(const IndexedModel& model)
{
	
	drawCount = model.indices.size();

	glGenVertexArrays(1, &vertexArrayObject); 
	glBindVertexArray(vertexArrayObject); 

	glGenBuffers(NUM_BUFFERS, vertexArrayBuffers);
	
	// Positions - location 0
	glBindBuffer(GL_ARRAY_BUFFER, vertexArrayBuffers[0]);
	glBufferData(GL_ARRAY_BUFFER, model.positions.size() * sizeof(model.positions[0]), &model.positions[0], GL_STATIC_DRAW);
	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 0, 0);

	// Normals - location 1
	glBindBuffer(GL_ARRAY_BUFFER, vertexArrayBuffers[NORMAL_VB]);
	glBufferData(GL_ARRAY_BUFFER, sizeof(model.normals[0]) * model.normals.size(), &model.normals[0], GL_STATIC_DRAW);
	glEnableVertexAttribArray(1);
	glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 0, 0);

	// TexCoords - location 2
	glBindBuffer(GL_ARRAY_BUFFER, vertexArrayBuffers[TEXCOORD_VB]);
	glBufferData(GL_ARRAY_BUFFER, model.texCoords.size() * sizeof(model.texCoords[0]), &model.texCoords[0], GL_STATIC_DRAW);
	glEnableVertexAttribArray(2);
	glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, 0, 0);

	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, vertexArrayBuffers[INDEX_VB]);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, model.indices.size() * sizeof(model.indices[0]), &model.indices[0], GL_STATIC_DRAW);

	glBindVertexArray(0);
}

Mesh::Mesh()
{
	drawCount = NULL;
}

void Mesh::loadModel(const std::string& filename)
{
	IndexedModel model = OBJModel(filename).ToIndexedModel();
	initModel(model);
}

Mesh::~Mesh()
{
	glDeleteVertexArrays(1, &vertexArrayObject); // delete arrays
}

void Mesh::draw()
{
	glBindVertexArray(vertexArrayObject);
	
	glDrawElements(GL_TRIANGLES, drawCount, GL_UNSIGNED_INT, 0);
	
	glBindVertexArray(0);
}

