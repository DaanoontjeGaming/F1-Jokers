function allowDrop(ev) { ev.preventDefault(); }

function drag(ev) { ev.dataTransfer.setData("text", ev.target.id); }

function drop(ev) {
    ev.preventDefault();
    const data = ev.dataTransfer.getData("text");
    if (!data) return;

    const draggedElement = document.getElementById(data);
    const target = ev.target;
    const dropZone = target.closest(".drop-target");
    const pool = target.closest(".driver-list");

    if (!draggedElement) return;

    if (pool && data.startsWith("clone-")) {
        const originalId = data.replace("clone-", "").split("-target")[0];
        const originalElement = document.getElementById(originalId);
        if (originalElement) originalElement.classList.remove("hide-section");

        const parentZone = draggedElement.parentNode;
        parentZone.innerHTML = "...";
        draggedElement.remove();
        return;
    }

    if (dropZone) {
        if (dropZone.children.length > 0 && dropZone.innerText !== "...") {
            const existingClone = dropZone.children[0];
            if (existingClone && existingClone.id && existingClone.id.startsWith("clone-")) {
                const existingOriginalId = existingClone.id.replace("clone-", "").split("-target")[0];
                const existingOriginal = document.getElementById(existingOriginalId);
                if (existingOriginal) existingOriginal.classList.remove("hide-section");
            }
        }

        if (data.startsWith("clone-")) {
            dropZone.innerHTML = "";
            dropZone.appendChild(draggedElement);
        } else {
            const clone = draggedElement.cloneNode(true);
            clone.id = "clone-" + data + "-target-" + dropZone.id;
            clone.setAttribute("ondragstart", "drag(event)");

            const originalDataId = draggedElement.getAttribute('data-id');
            if (originalDataId) {
                clone.setAttribute('data-id', originalDataId);
            }

            dropZone.innerHTML = "";
            dropZone.appendChild(clone);
            draggedElement.classList.add("hide-section");
        }
    }
}

function switchView(tabId) {
    document.querySelectorAll(".race-section").forEach(s => s.classList.add("hide-section"));
    const activeSection = document.getElementById(tabId);
    if (activeSection) activeSection.classList.remove("hide-section");

    document.querySelectorAll(".race-pool").forEach(p => p.classList.add("hide-section"));
    if (tabId === "main-race") {
        const poolMain = document.getElementById("pool-main");
        if (poolMain) poolMain.classList.remove("hide-section");
    } else if (tabId === "sprint-race") {
        const poolSprint = document.getElementById("pool-sprint");
        if (poolSprint) poolSprint.classList.remove("hide-section");
    } else if (tabId === "season-drivers") {
        const poolSDrivers = document.getElementById("pool-season-drivers");
        if (poolSDrivers) poolSDrivers.classList.remove("hide-section");
    } else if (tabId === "season-teams") {
        const poolSTeams = document.getElementById("pool-season-teams");
        if (poolSTeams) poolSTeams.classList.remove("hide-section");
    }

    document.querySelectorAll(".btn-tab").forEach(b => b.classList.remove("active"));
    if (event) event.currentTarget.classList.add("active");

    // HET PROBLEEM IS HIER VERWIJDERD! JavaScript past de deadline tekst niet meer aan.
}

function toggleAccessibilityMode() {
    const container = document.getElementById('voorspel-container');
    if (!container) return;

    container.classList.toggle('force-accessible');
    const btn = document.getElementById('btn-toggle-ui');

    if (btn) {
        if (container.classList.contains('force-accessible')) {
            btn.innerHTML = '<i class="bi bi-mouse"></i> Wissel naar Drag & Drop';
            btn.classList.remove('btn-info');
            btn.classList.add('btn-warning');
        } else {
            btn.innerHTML = '<i class="bi bi-universal-access"></i> Wissel naar Toegankelijk Voorspellen (Dropdowns)';
            btn.classList.remove('btn-warning');
            btn.classList.add('btn-info');
        }
    }
}

function getValuesFromSlots(desktopPrefix, count, mobilePrefix) {
    let result = [];

    let targetClass = '';
    if (mobilePrefix === 'mobile-race-pos') {
        targetClass = 'mobile-select-main';
    } else if (mobilePrefix === 'mobile-sprint-pos') {
        targetClass = 'mobile-select-sprint';
    } else if (mobilePrefix === 'mobile-sdriver-pos') {
        targetClass = 'mobile-select-sdrivers';
    } else if (mobilePrefix === 'mobile-steam-pos') {
        targetClass = 'mobile-select-steams';
    }

    const selects = document.querySelectorAll(`select.${targetClass}`);
    let foundMobileValues = false;
    selects.forEach((select) => {

        const value = select.value ? select.value.trim() : "";
        if (value !== "") foundMobileValues = true;
        result.push(value);
    });

    const container = document.getElementById('voorspel-container');
    const isAccessibleMode = container && container.classList.contains('force-accessible');
    const isMobileView = window.innerWidth < 768;

    if (!isAccessibleMode && !isMobileView) {
        result = [];
        const allDropTargets = document.querySelectorAll('div.drop-target');
        const relevantDropTargets = [];

        allDropTargets.forEach(dropZone => {
            if (dropZone.id && dropZone.id.startsWith(desktopPrefix)) {
                relevantDropTargets.push(dropZone);
            }
        });

        relevantDropTargets.sort((a, b) => {
            const numA = parseInt(a.id.replace(desktopPrefix, '')) || 0;
            const numB = parseInt(b.id.replace(desktopPrefix, '')) || 0;
            return numA - numB;
        });

        relevantDropTargets.forEach((dropZone) => {
            let startnr = "";
            if (dropZone.children.length > 0) {
                const card = dropZone.children[0];
                if (card.tagName === 'LI' && card.classList &&
                    (card.classList.contains('driver-card') || card.classList.contains('team-card'))) {

                    if (card.hasAttribute('data-id')) {
                        startnr = card.getAttribute('data-id');
                    } else if (card.id && card.id.includes('-c-')) {
                        const match = card.id.match(/-c-(\d+)/);
                        if (match && match[1]) {
                            startnr = match[1];
                        }
                    } else {
                        const numberSpan = card.querySelector('.dr-number');
                        if (numberSpan && numberSpan.innerText) {
                            startnr = numberSpan.innerText.trim();
                        }
                    }
                }
            }
            result.push(startnr);
        });

        while (result.length < count) {
            result.push("");
        }
    }

    return result.join(",");
}

async function verstuurVoorspelling() {
    const raceSelector = document.querySelector('select[name="raceId"]');
    if (!raceSelector) {
        alert("Let op: Race dropdown bovenaan de pagina ontbreekt in je code.");
        return;
    }

    const data = {
        RaceId: raceSelector.value,
        RaceTop10: getValuesFromSlots("race-pos", 10, "mobile-race-pos"),
        SprintTop5: getValuesFromSlots("sprint-pos", 5, "mobile-sprint-pos"),
        SeizoenCoureursTop10: getValuesFromSlots("sdriver-pos", 22, "mobile-sdriver-pos"),
        SeizoenTeamsTop11: getValuesFromSlots("steam-pos", 11, "mobile-steam-pos"),
        PolePositionStartnr: document.getElementById("bonus-pole")?.value || null,
        SnelsteRondeStartnr: document.getElementById("bonus-fastest")?.value || null,
        SprintPoleStartnr: document.getElementById("bonus-sprint-pole")?.value || null,
        MeesteRaceWinstStartnr: document.getElementById("season-most-wins")?.value || null,
        MeesteSprintWinstStartnr: document.getElementById("season-most-sprint-wins")?.value || null,
        MeesteRacePolesStartnr: document.getElementById("season-most-poles")?.value || null,
        MeesteSprintPolesStartnr: document.getElementById("season-most-sprint-poles")?.value || null
    };

    try {
        const response = await fetch("/Voorspelling/Opslaan", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data)
        });

        const serverResultaat = await response.json();

        if (response.ok) {
            alert(serverResultaat.message || "Je voorspelling is succesvol opgeslagen!");
            location.reload();
        } else {
            alert("Fout van server: " + serverResultaat.message);
        }
    } catch (error) {
        console.error("Fout bij verzenden naar de server:", error);
        alert("Er ging iets mis met de verbinding naar de server.");
    }
}