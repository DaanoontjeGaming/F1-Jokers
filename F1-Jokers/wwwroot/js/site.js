/* --- DRAG AND DROP LOGICA --- */
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
    const target = ev.target;
    const dropZone = target.closest(".drop-target");
    const pool = target.closest("#pool");

    /* Terugslepen naar de pool */
    if (pool && data.startsWith("clone-")) {
        const originalId = data.split('-')[1];
        const originalElement = document.getElementById(originalId);
        if (originalElement) {
            originalElement.style.display = "flex";
        }
        draggedElement.parentNode.classList.remove("filled");
        draggedElement.parentNode.innerHTML = "Sleep hier...";
        return;
    }

    /* Slepen naar een vakje */
    if (dropZone) {
        const activeSection = dropZone.closest(".race-section");
        const isSprint = activeSection && activeSection.id === "sprint-race";

        /* Als we een bestaande clone verplaatsen */
        if (data.startsWith("clone-")) {
            dropZone.innerHTML = "";
            dropZone.appendChild(draggedElement);
        }
        /* Als we een nieuwe coureur uit de lijst trekken */
        else {
            const clone = draggedElement.cloneNode(true);
            clone.id = "clone-" + data + "-" + dropZone.id;
            clone.setAttribute("ondragstart", "drag(event)");

            dropZone.innerHTML = "";
            dropZone.appendChild(clone);
            draggedElement.style.display = "none";
        }

        dropZone.classList.add("filled");
    }
}

/* --- RACE SWITCHER LOGICA --- */
function switchRace(raceType) {
    const mainRace = document.getElementById('main-race');
    const sprintRace = document.getElementById('sprint-race');
    const driverCards = document.querySelectorAll('#pool .driver-card');

    mainRace.classList.add('d-none');
    sprintRace.classList.add('d-none');

    const activeSection = document.getElementById(raceType);
    activeSection.classList.remove('d-none');

    driverCards.forEach(card => {
        card.style.display = "flex";
    });

    const filledSlots = activeSection.querySelectorAll('.drop-target.filled .driver-card');
    filledSlots.forEach(clone => {
        const originalId = clone.id.split('-')[1];
        const originalElement = document.getElementById(originalId);
        if (originalElement) {
            originalElement.style.display = "none";
        }
    });

    const buttons = document.querySelectorAll('.btn-tab');
    buttons.forEach(btn => btn.classList.remove('active'));

    if (raceType === 'main-race') {
        buttons[0].classList.add('active');
    } else {
        buttons[1].classList.add('active');
    }
}