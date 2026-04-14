function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {
    ev.dataTransfer.setData("text", ev.target.id);
}

function drop(ev) {
    ev.preventDefault();
    const data = ev.dataTransfer.getData("text");
    const draggedElement = document.getElementById(data);
    let target = ev.target;

    const dropZone = target.closest(".drop-target");
    const pool = document.getElementById("pool");

    if (dropZone) {
        const existingDriver = dropZone.querySelector(".driver-card");
        if (existingDriver && existingDriver !== draggedElement) {
            pool.appendChild(existingDriver);
        }
        dropZone.innerHTML = "";
        dropZone.appendChild(draggedElement);
        dropZone.classList.add("filled");
    }
    else if (target.id === "pool" || target.closest("#pool")) {
        pool.appendChild(draggedElement);
    }
    resetEmptySlots();
}

function resetEmptySlots() {
    const slots = document.querySelectorAll('.drop-target');
    slots.forEach(slot => {
        if (slot.children.length === 0) {
            slot.innerHTML = "Sleep hier...";
            slot.classList.remove("filled");
        }
    });
}