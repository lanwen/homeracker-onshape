// With edge chamfers off, every sloped face must be a pin-hole entrance.
annotation { "Feature Type Name" : "Panel chamfer checks" }
export const panelChamferChecks = defineFeature(function(context is Context, id is Id, definition is map)
    precondition {}
    {
        const layouts = [
                { "width" : 2, "height" : 2, "corners" : true, "allMounts" : true },
                { "width" : 4, "height" : 3, "corners" : true, "allMounts" : true },
                { "width" : 4, "height" : 4, "corners" : false, "allMounts" : true },
                { "width" : 4, "height" : 4, "corners" : true, "allMounts" : false },
                { "width" : 4, "height" : 4, "corners" : false, "allMounts" : false,
                  "noMounts" : true }];
        var index = 0;
        for (var fit in [PanelFit.INTERFIT, PanelFit.FULL_COVER])
            for (var layout in layouts)
            {
                const caseId = id + ("case" ~ index);
                homeracker(context, caseId, {
                        "kind" : HomerackerKind.PANEL, "panelFit" : fit,
                        "panelX" : layout.width, "panelY" : layout.height,
                        "cornerMounts" : layout.corners,
                        "northMounts" : layout.noMounts != true,
                        "eastMounts" : layout.noMounts != true,
                        "southMounts" : layout.allMounts, "westMounts" : layout.allMounts,
                        "panelChamfers" : false });
                const parts = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);
                if (size(evaluateQuery(context, parts)) != 1)
                    throw regenError("Panel chamfer case must produce one solid");
                const mates = evaluateQuery(context, qMateConnectorsOfParts(parts));
                var diagonalFaces = 0;
                for (var face in evaluateQuery(context, qOwnedByBody(parts, EntityType.FACE)))
                {
                    const normal = evPlane(context, { "face" : face }).normal;
                    var components = 0;
                    for (var axis = 0; axis < 3; axis += 1)
                        if (abs(normal[axis]) > 0.000001)
                            components += 1;
                    if (components < 2)
                        continue;
                    diagonalFaces += 1;
                    const bounds = evBox3d(context, { "topology" : face, "tight" : true });
                    const center = (bounds.minCorner + bounds.maxCorner) / 2;
                    var atPinEntrance = false;
                    for (var mate in mates)
                    {
                        const frame = evMateConnector(context, { "mateConnector" : mate });
                        const local = fromWorld(frame) * center;
                        const margin = 0.000001 * millimeter;
                        if (abs(local[0]) <= PIN / 2 + PIN_BEVEL + margin &&
                                abs(local[1]) <= PIN / 2 + PIN_BEVEL + margin &&
                                local[2] >= -WALL - margin &&
                                local[2] <= -WALL + PIN_BEVEL + margin)
                            atPinEntrance = true;
                    }
                    if (!atPinEntrance)
                        throw regenError("A panel edge is still chamfered outside a pin hole");
                }
                if (diagonalFaces != 4 * size(mates))
                    throw regenError("A panel pin-hole entrance lost its four bevel faces");
                println({ "fit" : fit, "layout" : index, "pinHoles" : size(mates),
                        "pinBevelFaces" : diagonalFaces, "otherBevelFaces" : 0 });
                opTransform(context, caseId + "place", { "bodies" : parts,
                        "transform" : transform(vector(index * 100, 0, 0) * millimeter) });
                index += 1;
            }
    });
