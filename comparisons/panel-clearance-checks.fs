annotation { "Feature Type Name" : "Panel clearance checks" }
export const panelClearanceChecks = defineFeature(function(context is Context, id is Id, definition is map)
    precondition {}
    {
        const horizontal = [PanelCornerClearance.HORIZONTAL, PanelCornerClearance.HORIZONTAL,
                            PanelCornerClearance.HORIZONTAL, PanelCornerClearance.HORIZONTAL];
        const ranges = [{ "clearanceSide" : PanelEdge.NORTH, "clearanceStart" : 9, "clearanceLength" : 3 }];
        const spec = panelClearanceSpec(14, 6, horizontal, ranges);
        if (!spec.custom || spec.edges[0] != [true, false, false, false, false, false, false, false,
                                             true, true, true, false, false, true] ||
                spec.edges[2] != [false, false, false, false, false, false] ||
                spec.edges[3] != [false, false, false, false, false, false])
            throw regenError("Directional corners or clearance positions are incorrect");
        const both = [PanelCornerClearance.BOTH, PanelCornerClearance.BOTH,
                      PanelCornerClearance.BOTH, PanelCornerClearance.BOTH];
        if (panelClearanceSpec(14, 6, both, []).custom)
            throw regenError("Default panel must retain its original mount layout");
        for (var invalid in [
                { "clearanceSide" : PanelEdge.NORTH, "clearanceStart" : 13, "clearanceLength" : 3 },
                { "clearanceSide" : PanelEdge.WEST, "clearanceStart" : 9, "clearanceLength" : 3 },
                { "clearanceSide" : PanelEdge.NORTH, "clearanceStart" : 0, "clearanceLength" : 1 }])
            if (try silent(panelClearanceSpec(14, 6, both, [invalid])) != undefined)
                throw regenError("Invalid edge clearance range accepted");
        for (var fit in [PanelFit.INTERFIT, PanelFit.FULL_COVER])
        {
            const caseId = id + ("panel" ~ fit);
            homeracker(context, caseId, {
                    "kind" : HomerackerKind.PANEL, "panelX" : 14, "panelY" : 6, "panelFit" : fit,
                    "cornerMounts" : false, "panelChamfers" : false,
                    "panelCornerNW" : horizontal[0], "panelCornerNE" : horizontal[1],
                    "panelCornerSW" : horizontal[2], "panelCornerSE" : horizontal[3],
                    "panelEdgeClearances" : ranges });
            const parts = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);
            const mates = evaluateQuery(context, qMateConnectorsOfParts(parts));
            if (size(evaluateQuery(context, parts)) != 1 || size(mates) != 33)
                throw regenError("Custom panel must remain one solid with 33 mount points");
            const topWall = (6 * U - 2 * WALL - FIT) / 2 + WALL + FIT / 2;
            for (var mate in mates)
            {
                const frame = evMateConnector(context, { "mateConnector" : mate });
                if (frame.zAxis[1] > 0.5 && abs(frame.origin[1] - topWall) < 0.000001 * millimeter &&
                        frame.origin[0] > 15 * millimeter && frame.origin[0] < 60 * millimeter)
                    throw regenError("A mount point remains in the top connector clearance");
            }
            println({ "fit" : fit, "oneSolid" : true, "mateCount" : size(mates), "clearance" : "Top 9 +3" });
            if (fit == PanelFit.FULL_COVER)
                opTransform(context, caseId + "place", { "bodies" : parts,
                        "transform" : transform(vector(250, 0, 0) * millimeter) });
        }
        var index = 0;
        for (var fit in [PanelFit.INTERFIT, PanelFit.FULL_COVER])
            for (var depths in [[0, 0, 0, 0], [1, 1, 1, 1], [1, 2, 3, 0], [3, 3, 3, 3], [2.1, 2.1, 2.1, 2.1]])
            {
                const caseId = id + ("depthCase" ~ index);
                homeracker(context, caseId, {
                        "kind" : HomerackerKind.PANEL, "panelX" : 4, "panelY" : 4, "panelFit" : fit,
                        "panelChamfers" : index % 2 == 0 || depths[0] == 2.1,
                        "panelEdgeClearances" : depths[0] == 2.1 ?
                                [{ "clearanceSide" : PanelEdge.NORTH, "clearanceStart" : 2, "clearanceLength" : 1 }] : [],
                        "panelInsetTop" : depths[0] * millimeter, "panelInsetBottom" : depths[1] * millimeter,
                        "panelInsetLeft" : depths[2] * millimeter, "panelInsetRight" : depths[3] * millimeter });
                const parts = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);
                const mates = evaluateQuery(context, qMateConnectorsOfParts(parts));
                if (size(evaluateQuery(context, parts)) != 1 || size(mates) != 16)
                    throw regenError("Inset depth case must remain one solid with 16 mount points");
                for (var mate in mates)
                {
                    const frame = evMateConnector(context, { "mateConnector" : mate });
                    if (norm(frame.zAxis) < 0.99 || norm(frame.xAxis) < 0.99)
                        throw regenError("An inset mount point has an invalid coordinate system");
                }
                println({ "fit" : fit, "depths" : depths, "oneSolid" : true, "mateCount" : size(mates) });
                opTransform(context, caseId + "place", { "bodies" : parts,
                        "transform" : transform(vector(index * 80, 150, 0) * millimeter) });
                index += 1;
            }
        for (var fit in [PanelFit.INTERFIT, PanelFit.FULL_COVER])
            for (var corner = 0; corner < 4; corner += 1)
            {
                var modes = both;
                const horizontalOnly = corner % 2 == 0;
                modes[corner] = horizontalOnly ? PanelCornerClearance.HORIZONTAL : PanelCornerClearance.VERTICAL;
                const caseId = id + ("endChamfer" ~ index);
                homeracker(context, caseId, {
                        "kind" : HomerackerKind.PANEL, "panelX" : 4, "panelY" : 4, "panelFit" : fit,
                        "cornerMounts" : false, "panelChamfers" : true,
                        "panelCornerNW" : modes[0], "panelCornerNE" : modes[1],
                        "panelCornerSW" : modes[2], "panelCornerSE" : modes[3] });
                const parts = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);
                const chamferFaces = qCreatedBy(caseId + "clearance" + "outlineChamfer", EntityType.FACE);
                if (size(evaluateQuery(context, parts)) != 1 || size(evaluateQuery(context, chamferFaces)) < 3)
                    throw regenError("A directional corner is missing its joined outline chamfer");
                opTransform(context, caseId + "place", { "bodies" : parts,
                        "transform" : transform(vector((index - 10) * 80, 250, 0) * millimeter) });
                index += 1;
            }
    });

annotation { "Feature Type Name" : "Panel corner join" }
export const panelCornerJoin = defineFeature(function(context is Context, id is Id, definition is map)
    precondition {}
    {
        homeracker(context, id + "panel", {
                "kind" : HomerackerKind.PANEL, "panelX" : 14, "panelY" : 6,
                "cornerMounts" : false, "panelChamfers" : true, "mateConnectors" : false,
                "panelCornerSE" : PanelCornerClearance.HORIZONTAL });
        const outline = id + "panel" + "clearance" + "outlineChamfer";
        if (size(evaluateQuery(context, qCreatedBy(outline, EntityType.FACE))) < 3)
            throw regenError("The panel needs a chamfer around its joined outline");
        const panelBody = qBodyType(qCreatedBy(id + "panel", EntityType.BODY), BodyType.SOLID);
        const left = -14 * U / 2 + WALL + FIT / 2;
        const bottom = -6 * U / 2 + WALL + FIT / 2;
        const shift = (U + FIT) / 2 + WALL / 2;
        var probeIndex = 0;
        // These three points lie in the unwanted cross and early-ended vertical bevel.
        for (var point in [vector(left + 0.25 * millimeter, bottom + 1.75 * millimeter, 3.9 * millimeter - shift),
                           vector(left + 1.75 * millimeter, bottom + 0.25 * millimeter, 3.9 * millimeter - shift),
                           vector(14 * U / 2 - 0.2 * millimeter, bottom + 0.2 * millimeter, 1.5 * millimeter - shift)])
        {
            const probe = id + ("probe" ~ probeIndex);
            makeBox(context, probe, point, vector(0.02, 0.02, 0.02) * millimeter);
            for (var clash in evCollision(context, { "tools" : body(probe), "targets" : panelBody }))
                if (clash["type"] == ClashType.INTERFERE || clash["type"] == ClashType.TOOL_IN_TARGET ||
                        clash["type"] == ClashType.TARGET_IN_TOOL)
                    throw regenError("Extra material interrupts the continuous corner chamfer");
            opDeleteBodies(context, probe + "delete", { "entities" : body(probe) });
            probeIndex += 1;
        }
        println("Corner chamfer witnesses: no cross and no early vertical end");
    });
