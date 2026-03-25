using System.Collections.Generic;
using UnityEngine;

public class QuadtreeExample : MonoBehaviour
{
    // Add GameObjects as children of this one in the editor to test (move them on the X-Y plane, keep Z as zero)
    [SerializeField] private Transform testObjectParent;
    [Min(1)] [SerializeField] private int maxObjectsPerNode = 2;
    [Min(0)] [SerializeField] private int maxDepth = 5;
    [SerializeField] private Vector2 quadtreeSize = new Vector2(100,100);
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private GameObject visualRectPrefab;
    [SerializeField] private bool displayQuadtreeBounds = true;

    private void Start()
    {
        // Instantiate a new quadtree, set the size to quadTreeSize, carrying over
        // maxObjectsPerNode and maxDepth from serialized variables set in the editor
        Quadtree myQuadtree = new Quadtree(new Rect(Vector2.zero, quadtreeSize), maxObjectsPerNode, maxDepth);
        
        CentreCameraOnQuadtree(myQuadtree);

        for (int i = 0; i < testObjectParent.childCount; i++)
        {
            myQuadtree.Insert(testObjectParent.GetChild(i).gameObject);
        }

        myQuadtree.Retrieve();

        if (displayQuadtreeBounds) DrawQuadtreeToCanvas(myQuadtree.root);
    }

    // Don't worry about this, just auto-centres camera regardless of quadtree size, making
    // it easier to test with different sizes
    private void CentreCameraOnQuadtree(Quadtree quadtree)
    {
        Camera cam = Camera.main;
        Rect quadtreeBounds = quadtree.root.bounds;
        cam.transform.position = new Vector3(quadtreeBounds.width/2f, quadtreeBounds.height/2f, -50f) + (Vector3)quadtreeBounds.position;

        float quadTreeAspect = quadtreeBounds.width / quadtreeBounds.height;

        if (cam.aspect > quadTreeAspect)
        {
            cam.orthographicSize = quadtreeBounds.height / 2f;
        }
        else
        {
            cam.orthographicSize = quadtreeBounds.width / cam.aspect / 2f;
        }
    }

    // Create a visual representation of the given node, then go through each of the children
    // of this node and do the same for them (goes down the tree recursively by calling itself)
    private void DrawQuadtreeToCanvas(QuadtreeNode node, Transform parentTransform)
    {     
        GameObject visualRect;
        visualRect = Instantiate(visualRectPrefab, parentTransform);
        visualRect.transform.position = node.bounds.position;
        RectTransform visualRectTransform = visualRect.GetComponent<RectTransform>();
        visualRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, node.bounds.width);
        visualRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, node.bounds.height);
        
        foreach (QuadtreeNode child in node.children)
        {
            DrawQuadtreeToCanvas(child, visualRect.transform);
        }
    }

    // If the function is called without a parentTransform argument, then a canvas is initialised
    // and is used as the root of the tree for the visual elements to show the quadtree bounds
    private void DrawQuadtreeToCanvas(QuadtreeNode node)
    {
        Canvas canvas = Instantiate(canvasPrefab).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
        canvasRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, quadtreeSize.x);
        canvasRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, quadtreeSize.y);
        DrawQuadtreeToCanvas(node, canvas.transform);
    }
}

// This class represents the entire Quadtree, with root node being the parent/grandparent/etc 
// to every other node within the tree. Functions are here if they should be called somewhere
// else to interact with the Quadtree, and if they need access to the root node
public class Quadtree
{
    // The node that the quadtree starts with, which all subsequent nodes will be children/grandchildren/etc of
    public QuadtreeNode root;
    // The number of objects that a node must surpass for it to split itself into quarters
    public int maxObjectsPerNode;
    // The maximum number of layers of splits down that will be done before no more splitting is done
    public int maxDepth;

    // Constructor method for this class, which takes the two int values,
    // and a rect value for the size that the root node should be
    public Quadtree(Rect rootNodeBounds, int maxObjectsPerNode, int maxDepth)
    {
        root = new QuadtreeNode()
        {
            bounds = rootNodeBounds,
            children = new QuadtreeNode[0],
            objects = new List<GameObject>()
        };

        this.maxObjectsPerNode = maxObjectsPerNode;
        this.maxDepth = maxDepth;
    }

    // Returns a bool which indicates whether the object could be inserted into the quadtree
    // at the specified node - this function calls itself recurssively, so it mostly uses this 
    // bool for its own benefit, to know if any of its subproccesses have succeeded so that
    // it can stop iterating through the list of children
    public bool Insert(GameObject gameObject, QuadtreeNode node, int currentDepth)
    {
        // If the given GameObject is not contained within the given node, return false
        if (!node.bounds.Contains(gameObject.transform.position)) { return false; }

        // If the given node has children, run this function recurssively on each child
        // until one of them returns true, in which case this function should also return true
        if (node.children.Length > 0)
        {
            foreach (QuadtreeNode child in node.children)
            {
                if (Insert(gameObject, child, currentDepth + 1))
                {
                    return true;
                }
            }
            return false;
        }

        // If the given node is already at the limit for the objects it can contain, split the
        // node into 4 children, and try to run this function on it again (now that it has 4
        // children, the previous if statement will be triggered instead)
        if (node.objects.Count >= maxObjectsPerNode && currentDepth < maxDepth)
        {
            node.Split();
            return Insert(gameObject, node, currentDepth);
        }

        // If the current node has 0 child nodes, has room for 1 more object, and the given object
        // overlaps with its bounds, then the object is added to the nodes 'objects' list
        node.objects.Add(gameObject);
        return true;
    }

    // If Insert() is called without specifying a node, then the root node will be used
    public bool Insert(GameObject gameObject)
    {
        return Insert(gameObject, root, 0);
    }

    // Returns a list of every single object through the entire node tree, descending
    // from the the specified node (only cares about this node and its children, grandchildren, etc.)
    public List<GameObject> Retrieve(QuadtreeNode node)
    {
        List<GameObject> retrievedObjects = new List<GameObject>();

        // If the given node doesn't overlap at all with the root node, return the list empty because
        // that node must not belong to this Quadtree (all nodes in a Quadtree overlap with the root)
        if (!node.bounds.Overlaps(root.bounds)) { return retrievedObjects; }

        // If the given node has children, run this method on each them to recursively retrieve every
        // object from every child descending from this node, and add each object retrieved to the list
        foreach (QuadtreeNode child in node.children)
        {
            foreach (GameObject currentObject in Retrieve(child))
            {
                retrievedObjects.Add(currentObject);
            }
        }

        // If the given node has objects stored, add each of them to the list, and print a message
        // to make sure that the system is working
        foreach (GameObject currentObject in node.objects)
        {
            retrievedObjects.Add(currentObject);
            Debug.Log($"{currentObject} is in node at pos {node.bounds.position}");
        }

        return retrievedObjects;
    }

    // If Retrieve() is called without specifying a node, the root node is used
    public List<GameObject> Retrieve()
    {
        return Retrieve(root);
    }
}

// This class represents each individual square of the quadtree
public class QuadtreeNode
{
    // Stores the width, height and position of the node (anchor is in the bottom left corner)
    public Rect bounds;
    // The objects within the node, which have to be registered with the Quadtree.Insert() function
    public List<GameObject> objects = new List<GameObject>();
    // The nodes that are direct children of this node. Usually either 4 or 0
    public QuadtreeNode[] children = new QuadtreeNode[0];

    // Splits this node into 4 child nodes
    public QuadtreeNode[] Split()
    {
        // Array of nodes with length of 4
        QuadtreeNode[] newChildren = new QuadtreeNode[4];
        // Creates 4 children, and adds each one to the array
        for (int i = 0; i < 4; i++)
        {
            QuadtreeNode currentChild = new QuadtreeNode();

            // This switch statement just makes sure that each rect is a different quarter of their parent node's bounds
            switch (i)
            {
                // Bottom left
                case 0:
                    currentChild.bounds = new Rect(bounds.position,bounds.size/2f);
                    break;
                // Bottom right
                case 1:
                    currentChild.bounds = new Rect(new Vector2(bounds.position.x + bounds.width/2f, bounds.position.y),bounds.size/2f);
                    break;
                // Top left
                case 2:
                    currentChild.bounds = new Rect(new Vector2(bounds.position.x, bounds.position.y + bounds.height/2f), bounds.size/2f);
                    break;
                // Top right
                case 3:
                    currentChild.bounds = new Rect(new Vector2(bounds.position.x + bounds.width/2f, bounds.position.y + bounds.height/2f), bounds.size/2f);
                    break;
            }
            // Adds newly created child to the array
            newChildren[i] = currentChild;
        }
        
        // Overwrites the array of length 0 stored in 'children' previously with a populated array of length 4
        children = newChildren;

        // For each object this node contains, check against each new child for which one it should be sent to
        foreach (GameObject gameObject in objects)
        {
            // Debug.Log($"looking at object {gameObject}");
            foreach (QuadtreeNode child in children)
            {
                if (child.bounds.Contains(gameObject.transform.position))
                {
                    // Debug.Log($"found to be overlapping node at pos {child.bounds.position}");
                    child.objects.Add(gameObject);
                }
            }
        }

        // Sets this nodes objects list to be empty. Leaving it populated could lead
        // to duplicates after just adding all of its contents to the new child nodes
        objects = new List<GameObject>();

        return children;
    }
}