class CustomGeometry extends THREE.Geometry { }

const geometry = new THREE.Geometry();
geometry.vertices.push(
    new THREE.Vector3(-1, -1, 1),  // 0
    new THREE.Vector3(1, -1, 1),  // 1
    new THREE.Vector3(-1, 1, 1),  // 2
    new THREE.Vector3(1, 1, 1),  // 3
    new THREE.Vector3(-1, -1, -1),  // 4
    new THREE.Vector3(1, -1, -1),  // 5
    new THREE.Vector3(-1, 1, -1),  // 6
    new THREE.Vector3(1, 1, -1),  // 7
);